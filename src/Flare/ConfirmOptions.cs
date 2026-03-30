namespace Flare;

/// <summary>
/// Per-call options for a confirm dialog. Any property left <c>null</c> falls back to the
/// corresponding value in <see cref="ConfirmDefaults"/>.
/// </summary>
public sealed class ConfirmOptions
{
    /// <summary>
    /// Text for the confirm button (e.g., "Delete", "Yes").
    /// </summary>
    public string? ConfirmText { get; set; }

    /// <summary>
    /// Text for the cancel button (e.g., "No", "Go back").
    /// </summary>
    public string? CancelText { get; set; }

    /// <summary>
    /// Visual intent of the confirm action. Use <see cref="ConfirmIntent.Danger"/> for destructive operations.
    /// </summary>
    public ConfirmIntent? Intent { get; set; }

    /// <summary>
    /// Which button receives initial focus. When <see cref="ConfirmIntent.Danger"/> is used,
    /// this defaults to <see cref="Flare.DefaultButton.Cancel"/> to prevent accidental confirmation.
    /// </summary>
    public DefaultButton? DefaultButton { get; set; }

    /// <summary>
    /// Whether pressing Escape closes the dialog (returning <c>false</c>).
    /// </summary>
    public bool? CloseOnEscape { get; set; }

    /// <summary>
    /// Whether clicking the backdrop closes the dialog (returning <c>false</c>).
    /// </summary>
    public bool? CloseOnBackdropClick { get; set; }

    /// <summary>
    /// Whether navigating to a different page closes the dialog (returning <c>false</c>).
    /// </summary>
    public bool? CloseOnNavigation { get; set; }
}
