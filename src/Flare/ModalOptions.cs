namespace Flare;

/// <summary>
/// Per-call options for a modal dialog. Any property left <c>null</c> falls back to the
/// corresponding value in <see cref="ModalDefaults"/>.
/// </summary>
public sealed class ModalOptions
{
    /// <summary>
    /// Title shown in the modal header. <c>null</c> falls back to the global default.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Additional CSS class(es) applied to the modal container.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Whether clicking the backdrop closes the modal.
    /// </summary>
    public bool? CloseOnBackdropClick { get; set; }

    /// <summary>
    /// Whether pressing Escape closes the modal.
    /// </summary>
    public bool? CloseOnEscape { get; set; }

    /// <summary>
    /// Whether to display the close (X) button in the modal header.
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// Whether navigating to a different page closes the modal (returning a cancel result).
    /// </summary>
    public bool? CloseOnNavigation { get; set; }

    /// <summary>
    /// Dictionary of parameters passed to the rendered component.
    /// Keys must match <c>[Parameter]</c> property names on the component.
    /// </summary>
    public Dictionary<string, object?>? Parameters { get; set; }
}
