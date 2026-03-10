using Microsoft.AspNetCore.Components;

namespace Flare;

public partial class FlareLoadingBar : ComponentBase
{
    [Parameter] public bool Active { get; set; }
    [Parameter] public int? Progress { get; set; }
    [Parameter] public bool Fixed { get; set; }
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
