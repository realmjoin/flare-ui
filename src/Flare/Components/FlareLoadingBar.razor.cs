using Microsoft.AspNetCore.Components;

namespace Flare;

/// <summary>
/// A loading bar component for declarative usage in Razor markup.
/// For imperative usage, see <see cref="IFlareService.StartLoadingBar"/>.
/// </summary>
/// <example>
/// <code>
/// &lt;FlareLoadingBar Active="_loading" Fixed /&gt;
/// &lt;FlareLoadingBar Active="_loading" Progress="_percent" /&gt;
/// </code>
/// </example>
public partial class FlareLoadingBar : ComponentBase
{
    /// <summary>
    /// Whether the loading bar is currently visible.
    /// </summary>
    [Parameter] public bool Active { get; set; }

    /// <summary>
    /// Progress percentage (0–100). When <c>null</c>, the bar shows an indeterminate animation.
    /// </summary>
    [Parameter] public int? Progress { get; set; }

    /// <summary>
    /// When <c>true</c>, the bar is fixed to the top of the viewport.
    /// </summary>
    [Parameter] public bool Fixed { get; set; }

    /// <summary>
    /// When <c>true</c>, built-in CSS classes are omitted so you can supply your own styles.
    /// </summary>
    [Parameter] public bool Headless { get; set; }

    private bool IsIndeterminate => Progress is null;

    private string? ContainerClass()
    {
        if (Headless) return null;
        return Fixed ? "flare-loading-bar flare-loading-bar-fixed" : "flare-loading-bar";
    }

    private string? BarClass()
    {
        if (Headless) return null;
        return IsIndeterminate ? "flare-loading-bar-track flare-loading-bar-indeterminate" : "flare-loading-bar-track";
    }

    private string? BarStyle()
    {
        if (Headless) return null;

        if (IsIndeterminate)
            return null;

        var clamped = Math.Clamp(Progress!.Value, 0, 100);
        return $"width: {clamped}%;";
    }
}
