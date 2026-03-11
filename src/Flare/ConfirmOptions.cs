namespace Flare;

public sealed class ConfirmOptions
{
    public string? ConfirmText { get; set; }
    public string? CancelText { get; set; }
    public ConfirmIntent? Intent { get; set; }
    public DefaultButton? DefaultButton { get; set; }
    public bool? CloseOnEscape { get; set; }
    public bool? CloseOnBackdropClick { get; set; }
}
