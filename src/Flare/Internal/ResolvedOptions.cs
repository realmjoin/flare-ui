using System.ComponentModel;
using Microsoft.AspNetCore.Components;

namespace Flare.Internal;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ResolvedModalOptions
{
    public string? Title { get; init; }
    public string? CssClass { get; init; }
    public bool CloseOnBackdropClick { get; init; }
    public bool CloseOnEscape { get; init; }
    public bool ShowCloseButton { get; init; }
    public Dictionary<string, object?>? Parameters { get; init; }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ResolvedConfirmOptions
{
    public required string ConfirmText { get; init; }
    public required string CancelText { get; init; }
    public ConfirmIntent Intent { get; init; }
    public DefaultButton DefaultButton { get; init; }
    public bool CloseOnEscape { get; init; }
    public bool CloseOnBackdropClick { get; init; }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ResolvedToastOptions
{
    public ToastLevel Level { get; init; }
    public int DurationMs { get; init; }
    public bool ShowProgress { get; init; }
    public bool PauseOnHover { get; init; }
    public bool Persistent { get; init; }
    public bool ShowCloseButton { get; init; }
    public RenderFragment? Content { get; init; }
    public Func<Task>? OnClick { get; init; }
}
