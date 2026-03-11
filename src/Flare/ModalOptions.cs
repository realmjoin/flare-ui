namespace Flare;

public sealed class ModalOptions
{
    public string? Title { get; set; }
    public string? CssClass { get; set; }
    public bool? CloseOnBackdropClick { get; set; }
    public bool? CloseOnEscape { get; set; }
    public bool? ShowCloseButton { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
}
