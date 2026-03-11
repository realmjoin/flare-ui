namespace Flare;

public sealed class FlareOptions
{
    public bool Headless { get; set; }
    public int MaxToasts { get; set; } = 5;
    public ToastPosition ToastPosition { get; set; } = ToastPosition.TopRight;
    public ModalDefaults Modal { get; set; } = new();
    public ConfirmDefaults Confirm { get; set; } = new();
    public ToastDefaults Toast { get; set; } = new();
}

public sealed class ModalDefaults
{
    public string? Title { get; set; }
    public string? CssClass { get; set; }
    public bool CloseOnBackdropClick { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;
    public bool ShowCloseButton { get; set; } = true;
}

public sealed class ConfirmDefaults
{
    public string ConfirmText { get; set; } = "OK";
    public string CancelText { get; set; } = "Cancel";
    public bool CloseOnEscape { get; set; } = true;
    public bool CloseOnBackdropClick { get; set; }
}

public sealed class ToastDefaults
{
    public ToastLevel Level { get; set; } = ToastLevel.Info;
    public int DurationMs { get; set; } = 5000;
    public bool ShowProgress { get; set; } = true;
    public bool PauseOnHover { get; set; } = true;
    public bool Persistent { get; set; }
    public bool ShowCloseButton { get; set; } = true;
}
