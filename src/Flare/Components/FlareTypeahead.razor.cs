using System.Linq.Expressions;
using Flare.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Flare;

/// <summary>
/// A single-value typeahead (autocomplete) control. Searches items as the user types and
/// allows selecting one result.
/// </summary>
/// <typeparam name="TItem">The type of items to search and select.</typeparam>
public partial class FlareTypeahead<TItem> : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The currently selected item.</summary>
    [Parameter] public TItem? Value { get; set; }

    /// <summary>Fires when the selected item changes.</summary>
    [Parameter] public EventCallback<TItem?> ValueChanged { get; set; }

    /// <summary>
    /// Expression identifying the bound value. Used for <see cref="EditForm"/> integration
    /// (validation CSS classes and field change notifications).
    /// </summary>
    [Parameter] public Expression<Func<TItem?>>? ValueExpression { get; set; }

    [CascadingParameter] private EditContext? EditContext { get; set; }

    /// <summary>
    /// Async function that returns matching items for the given search text.
    /// Either this or <see cref="Items"/> must be provided.
    /// </summary>
    [Parameter] public Func<string, CancellationToken, Task<IEnumerable<TItem>>>? SearchFunc { get; set; }

    /// <summary>
    /// Static list of items to filter client-side. Uses <see cref="TextSelector"/> for matching.
    /// </summary>
    [Parameter] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>
    /// Extracts display text from an item. Required.
    /// </summary>
    [Parameter, EditorRequired] public Func<TItem, string> TextSelector { get; set; } = default!;

    /// <summary>Custom template for rendering each dropdown item.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Placeholder text for the input.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Text shown while searching. Defaults to "Searching…".</summary>
    [Parameter] public string? LoadingText { get; set; }

    /// <summary>Text shown when no results match. Defaults to "No results found".</summary>
    [Parameter] public string? NotFoundText { get; set; }

    /// <summary>Debounce delay in milliseconds before triggering a search. Defaults to 300.</summary>
    [Parameter] public int DebounceMs { get; set; } = 300;

    /// <summary>Minimum characters before a search is triggered. Defaults to 1.</summary>
    [Parameter] public int MinLength { get; set; } = 1;

    /// <summary>
    /// Factory that creates a new <typeparamref name="TItem"/> from free text.
    /// When set, pressing Enter selects a custom value even if it doesn't appear in search results.
    /// When <c>null</c> (default), Enter selects the first matching result.
    /// </summary>
    [Parameter] public Func<string, TItem>? CreateItem { get; set; }

    /// <summary>Whether the control is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Additional CSS class(es) applied to the root container element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>When true, all built-in CSS classes are omitted.</summary>
    [Parameter] public bool Headless { get; set; }

    /// <summary>Additional HTML attributes splatted onto the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

    private ElementReference _root;
    private ElementReference _input;
    private IJSObjectReference? _module;
    private DotNetObjectReference<FlareTypeahead<TItem>>? _dotnetRef;
    private string? _jsId;

    private string _text = "";
    private List<TItem> _items = [];
    private int _highlightedIndex = -1;
    private bool _isOpen;
    private bool _loading;
    private bool _searched;
    private bool _disposed;
    private CancellationTokenSource? _searchCts;

    private readonly string _listboxId = $"flare-ta-list-{Guid.NewGuid():N}";
    private FieldIdentifier? _fieldIdentifier;

    protected override void OnParametersSet()
    {
        if (Value is not null)
        {
            var newText = GetText(Value);
            if (newText != _text && !_isOpen)
                _text = newText;
        }
        else if (!_isOpen)
        {
            _text = "";
        }

        _fieldIdentifier = EditContext is not null && ValueExpression is not null
            ? FieldIdentifier.Create(ValueExpression)
            : null;
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

            if (Value is not null)
                await SetValue(default);

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

            _items = results.ToList();
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
                await CommitCurrentInput();
                break;

            case "Escape":
                CloseDropdown();
                break;

            case "Tab":
                CloseDropdown();
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

        // If CreateItem is provided and there's text, create a custom value
        if (CreateItem is not null && !string.IsNullOrWhiteSpace(_text))
        {
            var newItem = CreateItem(_text.Trim());
            await SelectItem(newItem);
        }
    }

    private void HandleFocus()
    {
        if (_text.Length >= MinLength && !_isOpen)
        {
            _isOpen = true;
            _ = SearchAsync(_text);
        }
    }

    private void HandleBlur()
    {
        // Delay closing to allow pointerdown on option to fire first
        _ = Task.Run(async () =>
        {
            await Task.Delay(150);
            if (!_disposed)
                await InvokeAsync(() =>
                {
                    CloseDropdown();
                    StateHasChanged();
                });
        });
    }

    private async Task HandleClear()
    {
        _text = "";
        _items = [];
        _isOpen = false;
        _searched = false;
        _highlightedIndex = -1;

        if (Value is not null)
            await SetValue(default);

        try
        {
            if (_module is not null)
                await _module.InvokeVoidAsync("focusInput", _root);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
    }

    private async Task SelectItem(TItem item)
    {
        _text = GetText(item);
        CloseDropdown();
        await SetValue(item);
    }

    private async Task SetValue(TItem? item)
    {
        Value = item;
        await ValueChanged.InvokeAsync(item);
        if (_fieldIdentifier is { } fi)
            EditContext!.NotifyFieldChanged(fi);
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
                // Small yield so Blazor has time to render the aria-selected update
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
            CloseDropdown();
            StateHasChanged();
        });
    }

    private string GetText(TItem item) => TextSelector(item);

    private string? ActiveDescendant() =>
        _highlightedIndex >= 0 ? OptionId(_highlightedIndex) : null;

    private string OptionId(int index) => $"{_listboxId}-opt-{index}";

    private string? RootClass()
    {
        var validation = _fieldIdentifier is { } fi ? EditContext!.FieldCssClass(fi) : null;

        return (Headless, Class, validation) switch
        {
            (true, null or "", null or "") => null,
            (true, _, _) => Join(Class, validation),
            (false, null or "", null or "") => "flare-typeahead",
            _ => Join("flare-typeahead", Class, validation),
        };

        static string Join(params string?[] parts) =>
            string.Join(' ', parts.Where(p => !string.IsNullOrEmpty(p)));
    }
    private string? InputClass() => Headless ? null : "flare-typeahead-input";
    private string? ListboxClass() => Headless ? null : "flare-typeahead-dropdown";

    private string? OptionClass(int index) => Headless ? null :
        index == _highlightedIndex ? "flare-typeahead-option flare-typeahead-option-active" : "flare-typeahead-option";

    private Dictionary<string, object>? Attributes
    {
        get
        {
            if (InputAttributes is null) return null;
            var attrs = new Dictionary<string, object>(InputAttributes);
            attrs.Remove("class");
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
