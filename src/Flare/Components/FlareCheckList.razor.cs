using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace Flare;

/// <summary>
/// A searchable, scrollable multi-select check list with virtualized rendering.
/// Supports async remote search and server-side paging for large data sets.
/// </summary>
/// <typeparam name="TItem">The type of items to display and select.</typeparam>
public partial class FlareCheckList<TItem> : ComponentBase, IDisposable
{
    /// <summary>The currently selected items.</summary>
    [Parameter] public IReadOnlyList<TItem> Values { get; set; } = [];

    /// <summary>Fires when the selected items change.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TItem>> ValuesChanged { get; set; }

    /// <summary>
    /// Expression identifying the bound values. Used for <see cref="EditForm"/> integration.
    /// </summary>
    [Parameter] public Expression<Func<IReadOnlyList<TItem>>>? ValuesExpression { get; set; }

    [CascadingParameter] private EditContext? EditContext { get; set; }

    /// <summary>
    /// Async function that returns matching items for the given search text.
    /// Loads the full result set into memory. Good for up to ~5k items.
    /// Mutually exclusive with <see cref="Items"/> and <see cref="ProviderFunc"/>.
    /// </summary>
    [Parameter] public Func<string, CancellationToken, Task<IEnumerable<TItem>>>? SearchFunc { get; set; }

    /// <summary>
    /// Server-side paging provider. Returns a page of items and the total count.
    /// Only the visible slice is fetched — suitable for 100k+ rows.
    /// Parameters: (filter text, start index, page size, cancellation token).
    /// Mutually exclusive with <see cref="Items"/> and <see cref="SearchFunc"/>.
    /// </summary>
    [Parameter] public Func<string, int, int, CancellationToken, Task<CheckListResult<TItem>>>? ProviderFunc { get; set; }

    /// <summary>
    /// Static list of items to filter client-side. Uses <see cref="TextSelector"/> for matching.
    /// </summary>
    [Parameter] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>Extracts display text from an item. Required.</summary>
    [Parameter, EditorRequired] public Func<TItem, string> TextSelector { get; set; } = default!;

    /// <summary>
    /// Determines equality between items. Defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    [Parameter] public IEqualityComparer<TItem>? Comparer { get; set; }

    /// <summary>Custom template for rendering each row.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Extracts a unique key from an item for stable virtual scroll tracking.
    /// Defaults to <see cref="TextSelector"/> when not set.
    /// </summary>
    [Parameter] public Func<TItem, object>? KeySelector { get; set; }

    /// <summary>Placeholder text for the search input.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Text shown while loading. Defaults to "Loading…".</summary>
    [Parameter] public string? LoadingText { get; set; }

    /// <summary>Text shown when no items match. Defaults to "No items found".</summary>
    [Parameter] public string? NotFoundText { get; set; }

    /// <summary>Hint shown below the list when item count reaches <see cref="TruncatedThreshold"/>.</summary>
    [Parameter] public string? TruncatedText { get; set; }

    /// <summary>
    /// When <see cref="TruncatedText"/> is set, only show it if the loaded item count
    /// is greater than or equal to this value. Defaults to 0 (always show when TruncatedText is set).
    /// </summary>
    [Parameter] public int TruncatedThreshold { get; set; }

    /// <summary>Debounce delay in milliseconds before triggering a remote search. Defaults to 300.</summary>
    [Parameter] public int DebounceMs { get; set; } = 300;

    /// <summary>Whether to show the search/filter input. Defaults to true.</summary>
    [Parameter] public bool ShowSearch { get; set; } = true;

    /// <summary>Whether to show a "N selected" footer. Defaults to false.</summary>
    [Parameter] public bool ShowCount { get; set; }

    /// <summary>Sort selected items to the top of the list. Defaults to false.</summary>
    [Parameter] public bool SelectedFirst { get; set; }

    /// <summary>Maximum number of items that can be selected. 0 = unlimited. Defaults to 0.</summary>
    [Parameter] public int MaxItems { get; set; }

    /// <summary>
    /// CSS max-height for the scrollable list area (e.g. "20rem", "300px").
    /// When null, the list auto-expands to fit all visible items.
    /// </summary>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary>Row height in pixels for the virtualizer. Defaults to 40.</summary>
    [Parameter] public float ItemSize { get; set; } = 40;

    /// <summary>Whether the control is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Additional CSS class(es) applied to the root container element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>When true, all built-in CSS classes are omitted.</summary>
    [Parameter] public bool Headless { get; set; }

    /// <summary>Additional HTML attributes splatted onto the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

    private Virtualize<TItem>? _virtualizer;
    private readonly ItemsProviderDelegate<TItem> _itemsProvider;
    private string _filterText = "";
    private List<TItem> _cachedItems = [];
    private List<TItem> _selectedValues = [];
    private int _totalCount;
    private bool _loading;
    private bool _searched;
    private bool _needsRefresh;
    private CancellationTokenSource? _debounceCts;
    private FieldIdentifier? _fieldIdentifier;
    private List<TItem> _pinnedItems = [];
    private int _serverTotal;

    public FlareCheckList()
    {
        _itemsProvider = ProvideItems;
    }

    private IEqualityComparer<TItem> ItemComparer => Comparer ?? EqualityComparer<TItem>.Default;

    private bool IsAtLimit => MaxItems > 0 && _selectedValues.Count >= MaxItems;

    protected override void OnParametersSet()
    {
        if (!Values.SequenceEqual(_selectedValues, ItemComparer))
        {
            _selectedValues = Values.ToList();

            if (_searched && _selectedValues.Count > 0)
            {
                PatchMissingSelected();
                _needsRefresh = true;
            }
        }

        _fieldIdentifier = EditContext is not null && ValuesExpression is not null
            ? FieldIdentifier.Create(ValuesExpression)
            : null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_needsRefresh)
        {
            _needsRefresh = false;
            if (_virtualizer is not null)
            {
                await _virtualizer.RefreshDataAsync();
                StateHasChanged();
            }
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (ProviderFunc is null)
            await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _loading = true;

        try
        {
            if (SearchFunc is not null)
            {
                var results = await SearchFunc(_filterText, CancellationToken.None);
                _cachedItems = results.ToList();
            }
            else if (Items is not null)
            {
                _cachedItems = Items.ToList();
            }
            else
            {
                _cachedItems = [];
            }

            PatchMissingSelected();
            _totalCount = GetFilteredItems().Count;
            _searched = true;
        }
        catch (TaskCanceledException) { }
        catch
        {
            _cachedItems = [];
            _totalCount = 0;
            _searched = true;
        }
        finally
        {
            _loading = false;
            _needsRefresh = true;
        }
    }

    private ValueTask<ItemsProviderResult<TItem>> ProvideItems(ItemsProviderRequest request)
    {
        if (ProviderFunc is not null)
            return ProvideItemsFromServer(request);

        var items = GetFilteredItems();
        var page = items.Skip(request.StartIndex).Take(request.Count).ToList();
        return ValueTask.FromResult(new ItemsProviderResult<TItem>(page, items.Count));
    }

    private async ValueTask<ItemsProviderResult<TItem>> ProvideItemsFromServer(ItemsProviderRequest request)
    {
        _loading = true;

        try
        {
            if (_pinnedItems.Count == 0 && _selectedValues.Count > 0 && request.StartIndex == 0)
                _pinnedItems = _selectedValues.ToList();

            var pinCount = _pinnedItems.Count;
            var output = new List<TItem>();

            if (request.StartIndex < pinCount)
                output.AddRange(_pinnedItems.Skip(request.StartIndex).Take(request.Count));

            var remaining = request.Count - output.Count;
            if (remaining > 0)
            {
                var serverStart = Math.Max(0, request.StartIndex - pinCount);
                var pinnedSet = new HashSet<TItem>(_pinnedItems, ItemComparer);

                // Over-fetch to compensate for pinned items that will be deduped out.
                var fetchCount = remaining + pinnedSet.Count;
                var result = await ProviderFunc!(_filterText, serverStart, fetchCount, request.CancellationToken);
                if (request.CancellationToken.IsCancellationRequested)
                    return default;

                _serverTotal = result.TotalCount;
                output.AddRange(result.Items.Where(i => !pinnedSet.Contains(i)).Take(remaining));
            }

            _totalCount = pinCount + _serverTotal;
            _searched = true;
            _loading = false;
            return new ItemsProviderResult<TItem>(output, _totalCount);
        }
        catch (TaskCanceledException)
        {
            _loading = false;
            return default;
        }
        catch
        {
            _totalCount = 0;
            _loading = false;
            _searched = true;
            return new ItemsProviderResult<TItem>([], 0);
        }
    }

    private List<TItem> GetFilteredItems()
    {
        var items = _cachedItems;

        if (Items is not null && !string.IsNullOrEmpty(_filterText))
        {
            var q = _filterText;
            items = items.Where(i => GetText(i).Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (SelectedFirst)
        {
            var comparer = ItemComparer;
            var selected = items.Where(i => _selectedValues.Any(v => comparer.Equals(v, i))).ToList();
            var unselected = items.Where(i => !_selectedValues.Any(v => comparer.Equals(v, i))).ToList();
            return [.. selected, .. unselected];
        }

        return items;
    }

    private void PatchMissingSelected()
    {
        if (_selectedValues.Count == 0) return;
        var comparer = ItemComparer;
        var missing = _selectedValues
            .Where(v => !_cachedItems.Any(i => comparer.Equals(i, v)))
            .ToList();
        if (missing.Count > 0)
            _cachedItems.InsertRange(0, missing);
    }

    private async Task HandleFilterInput(ChangeEventArgs e)
    {
        _filterText = e.Value?.ToString() ?? "";

        if (ProviderFunc is null && SearchFunc is null)
        {
            _totalCount = GetFilteredItems().Count;
            _needsRefresh = true;
        }
        else
        {
            await DebouncedRefresh();
        }
    }

    private async Task DebouncedRefresh()
    {
        CancelDebounce();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            if (DebounceMs > 0)
                await Task.Delay(DebounceMs, token);

            if (!token.IsCancellationRequested)
            {
                _searched = false;
                _pinnedItems = [];
                _serverTotal = 0;

                if (ProviderFunc is null)
                    await LoadDataAsync();
                else
                    await RefreshVirtualizer();
            }
        }
        catch (TaskCanceledException) { }
    }

    private async Task RefreshVirtualizer()
    {
        if (_virtualizer is not null)
            await _virtualizer.RefreshDataAsync();

        StateHasChanged();
    }

    private void HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Escape" && !string.IsNullOrEmpty(_filterText))
            _ = ClearFilterAsync();
    }

    private async Task ClearFilter()
    {
        await ClearFilterAsync();
    }

    private async Task ClearFilterAsync()
    {
        _filterText = "";
        CancelDebounce();
        _searched = false;
        _pinnedItems = [];
        _serverTotal = 0;

        if (ProviderFunc is null)
            await LoadDataAsync();
        else
            await RefreshVirtualizer();
    }

    private async Task ToggleItem(TItem item)
    {
        if (Disabled) return;

        var comparer = ItemComparer;
        var idx = _selectedValues.FindIndex(v => comparer.Equals(v, item));

        if (idx >= 0)
        {
            _selectedValues.RemoveAt(idx);
        }
        else
        {
            if (IsAtLimit) return;
            _selectedValues.Add(item);
        }

        await ValuesChanged.InvokeAsync(_selectedValues.AsReadOnly());
        if (_fieldIdentifier is { } fi)
            EditContext!.NotifyFieldChanged(fi);

        if (SelectedFirst)
        {
            if (ProviderFunc is not null)
                _pinnedItems = [];
            _needsRefresh = true;
        }
    }

    private bool IsSelected(TItem item)
    {
        var comparer = ItemComparer;
        return _selectedValues.Any(v => comparer.Equals(v, item));
    }

    private void CancelDebounce()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;
    }

    private string GetText(TItem item) => TextSelector(item);

    private object GetKey(TItem item) => KeySelector is not null ? KeySelector(item) : GetText(item);

    private string? ListStyle() =>
        MaxHeight is not null ? $"max-height: {MaxHeight}; overflow-y: auto;" : null;

    private string? RootClass()
    {
        var validation = _fieldIdentifier is { } fi ? EditContext!.FieldCssClass(fi) : null;

        if (Headless)
            return Join(Class, validation);

        var root = Disabled ? "flare-checklist flare-checklist-disabled" : "flare-checklist";
        return Join(root, Class, validation);

        static string? Join(params string?[] parts)
        {
            var result = string.Join(' ', parts.Where(p => !string.IsNullOrEmpty(p)));
            return result.Length > 0 ? result : null;
        }
    }

    private string? RowClass(TItem item)
    {
        if (Headless) return null;
        var selected = IsSelected(item);
        var disabled = !selected && IsAtLimit;
        return (selected, disabled) switch
        {
            (true, _) => "flare-checklist-row flare-checklist-row-selected",
            (_, true) => "flare-checklist-row flare-checklist-row-disabled",
            _ => "flare-checklist-row",
        };
    }

    public void Dispose()
    {
        CancelDebounce();
    }
}
