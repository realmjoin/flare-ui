using Flare.Internal;
using Microsoft.AspNetCore.Components;

namespace Flare;

public partial class FlareLoadingToast : ComponentBase
{
    [Parameter, EditorRequired] public LoadingToastState Instance { get; set; } = default!;
    [Parameter] public bool Headless { get; set; }

    private string? CssClass()
    {
        if (Headless) return null;

        var exiting = Instance.IsExiting ? " flare-loading-toast-exiting" : "";
        return $"flare-loading-toast{exiting}";
    }
}
