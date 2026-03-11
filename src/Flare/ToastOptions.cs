using Microsoft.AspNetCore.Components;

namespace Flare;

/// <summary>
/// Per-call options for a toast notification. Any property left <c>null</c> falls back to the
/// corresponding value in <see cref="ToastDefaults"/>.
/// </summary>
public sealed class ToastOptions
{
    /// <summary>
    /// Severity level of the toast. Determines the visual style.
    /// </summary>
    public ToastLevel? Level { get; set; }

    /// <summary>
    /// How long the toast stays visible, in milliseconds.
    /// </summary>
    public int? DurationMs { get; set; }

    /// <summary>
    /// Whether to show a countdown progress bar on the toast.
    /// </summary>
    public bool? ShowProgress { get; set; }

    /// <summary>
    /// Whether hovering over the toast pauses the auto-dismiss timer.
    /// </summary>
    public bool? PauseOnHover { get; set; }

    /// <summary>
    /// When <c>true</c>, the toast stays visible until manually dismissed.
    /// </summary>
    public bool? Persistent { get; set; }

    /// <summary>
    /// Whether to display a close (X) button on the toast.
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// Optional custom Blazor content rendered inside the toast body.
    /// </summary>
    public RenderFragment? Content { get; set; }

    /// <summary>
    /// Callback invoked when the user clicks the toast body.
    /// </summary>
    public Func<Task>? OnClick { get; set; }
}
