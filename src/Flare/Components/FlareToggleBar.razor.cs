using System.Linq.Expressions;
using Flare.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Flare;

/// <summary>
/// A connected bar of segments for picking one option (default) or several
/// (<see cref="Multiple"/>) from a short list. Renders native radio buttons or
/// checkboxes, so keyboard handling and form semantics come from the browser.
/// </summary>
/// <typeparam name="TItem">The type of the options.</typeparam>
public partial class FlareToggleBar<TItem> : ComponentBase
{
    /// <summary>The options to show, in display order. Required.</summary>
    [Parameter, EditorRequired] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// When true, any number of options can be active and the selection is bound through
    /// <see cref="Values"/>. When false (default), exactly one option is active and the
    /// selection is bound through <see cref="Value"/>.
    /// </summary>
    [Parameter] public bool Multiple { get; set; }

    /// <summary>The selected option in single-select mode.</summary>
    [Parameter] public TItem? Value { get; set; }

    /// <summary>Fires when the selected option changes in single-select mode.</summary>
    [Parameter] public EventCallback<TItem?> ValueChanged { get; set; }

    /// <summary>
    /// Expression identifying the bound value. Used for <see cref="EditForm"/> integration.
    /// </summary>
    [Parameter] public Expression<Func<TItem?>>? ValueExpression { get; set; }

    /// <summary>The selected options in multi-select mode, in the order of <see cref="Items"/>.</summary>
    [Parameter] public IReadOnlyList<TItem> Values { get; set; } = [];

    /// <summary>Fires when the selected options change in multi-select mode.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TItem>> ValuesChanged { get; set; }

    /// <summary>
    /// Expression identifying the bound values. Used for <see cref="EditForm"/> integration.
    /// </summary>
    [Parameter] public Expression<Func<IReadOnlyList<TItem>>>? ValuesExpression { get; set; }

    [CascadingParameter] private EditContext? EditContext { get; set; }

    /// <summary>Extracts display text from an option. Defaults to <see cref="object.ToString"/>.</summary>
    [Parameter] public Func<TItem, string>? TextSelector { get; set; }

    /// <summary>
    /// Determines equality between options. Defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    [Parameter] public IEqualityComparer<TItem>? Comparer { get; set; }

    /// <summary>Custom template for the content of each segment.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// When true, the bar fills the available width and all segments share it equally.
    /// Defaults to false (the bar is as wide as its content).
    /// </summary>
    [Parameter] public bool Stretch { get; set; }

    /// <summary>Whether the control is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Additional CSS class(es) applied to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// When true, all built-in CSS classes are omitted and the native radio buttons or
    /// checkboxes stay visible.
    /// </summary>
    [Parameter] public bool Headless { get; set; }

    /// <summary>Additional HTML attributes splatted onto the root element (e.g. <c>aria-label</c>).</summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

    // Radio buttons are grouped by name; a per-instance name keeps two bars on a page apart.
    private readonly string _name = $"flare-togglebar-{Guid.NewGuid():N}";

    // The native inputs change their checked state on click, whatever the parent does with
    // the change. The component therefore keeps its own copy of the selection and only
    // takes over a parameter value when that value actually changed, so what is rendered
    // can never differ from what the inputs show.
    private bool _initialized;
    private TItem? _value;
    private TItem? _lastValueParameter;
    private List<TItem> _values = [];
    private List<TItem> _lastValuesParameter = [];
    private FieldIdentifier? _fieldIdentifier;

    private IEqualityComparer<TItem> ItemComparer => Comparer ?? EqualityComparer<TItem>.Default;

    protected override void OnParametersSet()
    {
        var comparer = ItemComparer;

        if (!_initialized || !comparer.Equals(Value!, _lastValueParameter!))
            _value = Value;
        _lastValueParameter = Value;

        IReadOnlyList<TItem> incoming = Values ?? [];
        if (!_initialized || !incoming.SequenceEqual(_lastValuesParameter, comparer))
            _values = incoming.ToList();
        _lastValuesParameter = incoming.ToList();

        _initialized = true;

        _fieldIdentifier = EditContext is null
            ? null
            : Multiple
                ? ValuesExpression is not null ? FieldIdentifier.Create(ValuesExpression) : null
                : ValueExpression is not null ? FieldIdentifier.Create(ValueExpression) : null;
    }

    private async Task SelectAsync(TItem item)
    {
        if (Disabled || ItemComparer.Equals(_value!, item)) return;

        _value = item;
        await ValueChanged.InvokeAsync(item);

        if (ValueChanged.HasDelegate)
        {
            await ParentRenderedAsync();
            if (!ItemComparer.Equals(Value!, item))
            {
                _value = Value;
                return;
            }
        }

        NotifyFieldChanged();
    }

    private async Task SetCheckedAsync(TItem item, ChangeEventArgs e)
    {
        if (Disabled) return;

        _values = ToggleBarSelection.Toggle(Items, _values, item, e.Value is true, ItemComparer);
        await ValuesChanged.InvokeAsync(_values.AsReadOnly());

        if (ValuesChanged.HasDelegate)
        {
            await ParentRenderedAsync();
            IReadOnlyList<TItem> bound = Values ?? [];
            if (!bound.SequenceEqual(_values, ItemComparer))
            {
                _values = bound.ToList();
                return;
            }
        }

        NotifyFieldChanged();
    }

    // Event handlers run inside a render batch, so right after the callback the parent has
    // not re-rendered yet. Yielding lets that batch finish: the parent has then passed down
    // what it made of the change, and this component has rendered the clicked state. If the
    // parent did not take the change over (e.g. it enforces a limit), going back to the bound
    // value afterwards is a real diff and resets the native input, which has already flipped.
    private static async Task ParentRenderedAsync() => await Task.Yield();

    private void NotifyFieldChanged()
    {
        if (_fieldIdentifier is { } fi)
            EditContext!.NotifyFieldChanged(fi);
    }

    private bool IsSelected(TItem item)
    {
        var comparer = ItemComparer;
        return Multiple
            ? _values.Any(v => comparer.Equals(v, item))
            : comparer.Equals(_value!, item);
    }

    private string GetText(TItem item) =>
        TextSelector is not null ? TextSelector(item) : item?.ToString() ?? "";

    private string? RootClass()
    {
        var validation = _fieldIdentifier is { } fi ? EditContext!.FieldCssClass(fi) : null;

        if (Headless)
            return Join(Class, validation);

        return Join(
            "flare-togglebar",
            Multiple ? "flare-togglebar-multiple" : "flare-togglebar-single",
            Stretch ? "flare-togglebar-stretch" : null,
            Disabled ? "flare-togglebar-disabled" : null,
            Class,
            validation);

        static string? Join(params string?[] parts)
        {
            var result = string.Join(' ', parts.Where(p => !string.IsNullOrEmpty(p)));
            return result.Length > 0 ? result : null;
        }
    }

    private string? ItemClass(bool selected)
    {
        if (Headless) return null;
        return selected ? "flare-togglebar-item flare-togglebar-item-selected" : "flare-togglebar-item";
    }
}
