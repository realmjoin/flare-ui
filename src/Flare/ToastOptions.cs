using Microsoft.AspNetCore.Components;

namespace Flare;

public sealed class ToastOptions
{
    public ToastLevel? Level { get; set; }
    public int? DurationMs { get; set; }
    public bool? ShowProgress { get; set; }
    public bool? PauseOnHover { get; set; }
    public bool? Persistent { get; set; }
    public bool? ShowCloseButton { get; set; }
    public RenderFragment? Content { get; set; }
    public Func<Task>? OnClick { get; set; }
}
