using Flare.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Flare;

/// <summary>
/// A multi-select tagging control with typeahead search. Users can search and select multiple
/// items that appear as removable tags.
/// </summary>
/// <typeparam name="TItem">The type of items to search and select.</typeparam>
public partial class FlareTagBox<TItem> : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The currently selected items.</summary>
    [Parameter] public IReadOnlyList<TItem> Values { get; set; } = [];

    /// <summary>Fires when the selected items change.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TItem>> ValuesChanged { get; set; }

    /// <summary>
    /// Async function that returns matching items for the given search text.
    /// Either this or <see cref="Items"/> must be provided.
    /// </summary>
    [Parameter] public Func<string, CancellationToken, Task<IEnumerable<TItem>>>? SearchFunc { get; set; }

    /// <summary>
    /// Static list of items to filter client-side. Uses <see cref="TextSelector"/> for matching.
    /// </summary>
    [Parameter] public IReadOnlyList<TItem>? Items { get; set; }

    /// <summary>
    /// Extracts display text from an item. Required.
    /// </summary>
    [Parameter, EditorRequired] public Func<TItem, string> TextSelector { get; set; } = default!;

    /// <summary>
    /// Determines equality between items. Defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// Used to prevent duplicate selections and filter already-selected items from results.
    /// </summary>
    [Parameter] public IEqualityComparer<TItem>? Comparer { get; set; }

    /// <summary>Custom template for rendering each dropdown item.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Custom template for rendering each selected tag.</summary>
    [Parameter] public RenderFragment<TItem>? TagTemplate { get; set; }

    /// <summary>Placeholder text shown when no tags are selected.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Text shown while searching. Defaults to "Searching…".</summary>
    [Parameter] public string? LoadingText { get; set; }

    /// <summary>Text shown when no results match. Defaults to "No results found".</summary>
    [Parameter] public string? NotFoundText { get; set; }

    /// <summary>Debounce delay in milliseconds before triggering a search. Defaults to 300.</summary>
    [Parameter] public int DebounceMs { get; set; } = 300;

    /// <summary>Minimum characters before a search is triggered. Defaults to 1.</summary>
    [Parameter] public int MinLength { get; set; } = 1;

    /// <summary>Maximum number of tags that can be selected. 0 = unlimited. Defaults to 0.</summary>
    [Parameter] public int MaxTags { get; set; }

    /// <summary>
    /// Factory that creates a new <typeparamref name="TItem"/> from free text.
    /// When set, pressing Enter or comma adds a custom tag even if it doesn't appear in search results.
    /// When <c>null</c> (default), Enter or comma selects the first matching result.
    /// </summary>
    [Parameter] public Func<string, TItem>? CreateItem { get; set; }

    /// <summary>Whether the control is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>When true, all built-in CSS classes are omitted.</summary>
    [Parameter] public bool Headless { get; set; }

    /// <summary>Additional HTML attributes splatted onto the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

    private ElementReference _root;
    private ElementReference _input;
    private IJSObjectReference? _module;
    private DotNetObjectReference<FlareTagBox<TItem>>? _dotnetRef;
    private string? _jsId;

    private List<TItem> _values = [];
    private string _text = "";
    private List<TItem> _items = [];
    private int _highlightedIndex = -1;
    private bool _isOpen;
    private bool _loading;
    private bool _searched;
    private bool _disposed;
    private CancellationTokenSource? _searchCts;

    private readonly string _listboxId = $"flare-tb-list-{Guid.NewGuid():N}";

    private IEqualityComparer<TItem> ItemComparer => Comparer ?? EqualityComparer<TItem>.Default;

    private bool IsDisabled => Disabled || (MaxTags > 0 && _values.Count >= MaxTags);

    protected override void OnParametersSet()
    {
        _values = Values.ToList();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_disposed)
        {
            try
            {
                _module = await JS.GetFlareTypeaheadModuleAsync();
                _dotnetRef = DotNetObjectReference.Create(this);
                _jsId = await _module.InvokeAsync<string>("init", _dotnetRef, _root);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
        }
    }

    private async Task HandleInput(ChangeEventArgs e)
    {
        _text = e.Value?.ToString() ?? "";
        _highlightedIndex = -1;

        if (_text.Length < MinLength)
        {
            _isOpen = false;
            _items = [];
            _searched = false;
            return;
        }

        _isOpen = true;
        await SearchAsync(_text);
    }

    private async Task SearchAsync(string query)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _loading = true;
        StateHasChanged();

        try
        {
            if (DebounceMs > 0)
                await Task.Delay(DebounceMs, token);

            IEnumerable<TItem> results;

            if (SearchFunc is not null)
            {
                results = await SearchFunc(query, token);
            }
            else if (Items is not null)
            {
                var q = query.ToLowerInvariant();
                results = Items.Where(item => GetText(item).Contains(q, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                results = [];
            }

            if (token.IsCancellationRequested) return;

            // Filter out already-selected items
            var comparer = ItemComparer;
            _items = results.Where(r => !_values.Any(v => comparer.Equals(v, r))).ToList();
            _searched = true;
            _loading = false;
            _highlightedIndex = -1;
        }
        catch (TaskCanceledException) { }
        catch
        {
            _items = [];
            _loading = false;
            _searched = true;
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowDown":
                if (!_isOpen && _text.Length >= MinLength)
                {
                    _isOpen = true;
                    await SearchAsync(_text);
                }
                else if (_items.Count > 0)
                {
                    _highlightedIndex = (_highlightedIndex + 1) % _items.Count;
                    ScrollToHighlighted();
                }
                break;

            case "ArrowUp":
                if (_items.Count > 0)
                {
                    _highlightedIndex = _highlightedIndex <= 0 ? _items.Count - 1 : _highlightedIndex - 1;
                    ScrollToHighlighted();
                }
                break;

            case "Enter":
            case ",":
                await CommitCurrentInput();
                break;

            case "Escape":
                CloseDropdown();
                break;

            case "Backspace":
                if (string.IsNullOrEmpty(_text) && _values.Count > 0)
                {
                    await RemoveTag(_values[^1]);
                }
                break;

            case "Tab":
                if (_highlightedIndex >= 0 && _highlightedIndex < _items.Count)
                {
                    await SelectItem(_items[_highlightedIndex]);
                }
                else
                {
                    CloseDropdown();
                }
                break;
        }
    }

    private async Task CommitCurrentInput()
    {
        // If an item is highlighted, select it
        if (_highlightedIndex >= 0 && _highlightedIndex < _items.Count)
        {
            await SelectItem(_items[_highlightedIndex]);
            return;
        }

        // If there are search results, select the first one
        if (_items.Count > 0)
        {
            await SelectItem(_items[0]);
            return;
        }

        // If CreateItem is provided and there's text, create a custom tag
        if (CreateItem is not null && !string.IsNullOrWhiteSpace(_text))
        {
            var trimmed = _text.Trim();
            var newItem = CreateItem(trimmed);

            // Skip duplicates
            var comparer = ItemComparer;
            if (_values.Any(v => comparer.Equals(v, newItem)))
            {
                _text = "";
                CloseDropdown();
                return;
            }

            await SelectItem(newItem);
        }
    }

    private void HandleFocusIn()
    {
        if (_text.Length >= MinLength && !_isOpen)
        {
            _isOpen = true;
            _ = SearchAsync(_text);
        }
    }

    private void HandleBlur()
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(150);
            if (!_disposed)
                await InvokeAsync(() =>
                {
                    _text = "";
                    CloseDropdown();
                    StateHasChanged();
                });
        });
    }

    private async Task HandleTagKeyDown(KeyboardEventArgs e, TItem item)
    {
        if (e.Key is "Delete" or "Backspace")
            await RemoveTag(item);
        else if (e.Key is "Enter")
            FocusInput();
    }

    private async Task SelectItem(TItem item)
    {
        _values.Add(item);
        _text = "";
        _items = [];
        _searched = false;
        CloseDropdown();
        await NotifyChanged();

        try
        {
            if (_module is not null)
                await _module.InvokeVoidAsync("focusInput", _root);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
    }

    private async Task RemoveTag(TItem item)
    {
        var comparer = ItemComparer;
        var idx = _values.FindIndex(v => comparer.Equals(v, item));
        if (idx >= 0)
        {
            _values.RemoveAt(idx);
            await NotifyChanged();
        }
    }

    private async Task NotifyChanged()
    {
        await ValuesChanged.InvokeAsync(_values.AsReadOnly());
    }

    private void FocusInput()
    {
        if (_module is null || Disabled) return;
        _ = Task.Run(async () =>
        {
            try { await _module.InvokeVoidAsync("focusInput", _root); }
            catch { }
        });
    }

    private void CloseDropdown()
    {
        _isOpen = false;
        _highlightedIndex = -1;
    }

    private void ScrollToHighlighted()
    {
        if (_module is null || _highlightedIndex < 0) return;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(10);
                var selector = $"#{OptionId(_highlightedIndex)}";
                var el = await JS.InvokeAsync<IJSObjectReference?>("document.querySelector", selector);
                if (el is not null)
                    await _module.InvokeVoidAsync("scrollIntoView", el);
            }
            catch { }
        });
    }

    [JSInvokable]
    public void OnClickOutside()
    {
        InvokeAsync(() =>
        {
            _text = "";
            CloseDropdown();
            StateHasChanged();
        });
    }

    private string GetText(TItem item) => TextSelector(item);

    private string? CurrentPlaceholder() =>
        _values.Count == 0 ? Placeholder : null;

    private string? ActiveDescendant() =>
        _highlightedIndex >= 0 ? OptionId(_highlightedIndex) : null;

    private string OptionId(int index) => $"{_listboxId}-opt-{index}";

    private string? RootClass() => Headless ? null : $"flare-tagbox{(Disabled ? " flare-tagbox-disabled" : "")}";
    private string? InputClass() => Headless ? null : "flare-tagbox-input";
    private string? ListboxClass() => Headless ? null : "flare-tagbox-dropdown";

    private string? OptionClass(int index) => Headless ? null :
        index == _highlightedIndex ? "flare-tagbox-option flare-tagbox-option-active" : "flare-tagbox-option";

    private Dictionary<string, object>? Attributes
    {
        get
        {
            if (InputAttributes is null) return null;
            var attrs = new Dictionary<string, object>(InputAttributes);
            attrs.Remove("disabled");
            attrs.Remove("placeholder");
            return attrs.Count > 0 ? attrs : null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _searchCts?.Cancel();
        _searchCts?.Dispose();

        if (_module is not null && _jsId is not null)
        {
            try { await _module.InvokeVoidAsync("dispose", _jsId); }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
        }

        _dotnetRef?.Dispose();
    }
}
