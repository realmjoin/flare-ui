namespace Flare;

public sealed class ConfirmOptions
{
    public string ConfirmText { get; set; } = "OK";
    public string CancelText { get; set; } = "Cancel";
    public bool CloseOnEscape { get; set; } = true;
    public bool CloseOnBackdropClick { get; set; }
}
