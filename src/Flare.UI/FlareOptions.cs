namespace Flare;

public sealed class FlareOptions
{
    public bool Headless { get; set; }
    public int MaxToasts { get; set; } = 5;
    public ToastPosition ToastPosition { get; set; } = ToastPosition.TopRight;
    public ToastOptions Toast { get; set; } = new();
    public ModalOptions Modal { get; set; } = new();
    public ConfirmOptions Confirm { get; set; } = new();
}
