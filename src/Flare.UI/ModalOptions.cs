namespace Flare;

public sealed class ModalOptions
{
    public string? Title { get; set; }
    public bool CloseOnBackdropClick { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;
    public bool ShowCloseButton { get; set; } = true;
    public Dictionary<string, object?>? Parameters { get; set; }
}
