namespace Flare;

public sealed class ToastOptions
{
    public ToastLevel Level { get; set; } = ToastLevel.Info;
    public int DurationMs { get; set; } = 5000;
    public bool ShowProgress { get; set; } = true;
    public bool PauseOnHover { get; set; } = true;
    public bool Persistent { get; set; }
    public bool ShowCloseButton { get; set; } = true;
    public Func<Task>? OnClick { get; set; }
}
