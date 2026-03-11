using Flare.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Flare;

public partial class FlareProvider : ComponentBase, IDisposable
{
    [Inject] private IFlareService Flare { get; set; } = default!;
    [Inject] private FlareOptions _options { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private FlareService _service => (FlareService)Flare;

    protected override void OnInitialized()
    {
        Flare.OnChanged += HandleChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try { await JS.GetFlareModuleAsync(); }
            catch { }
        }
    }

    private void HandleChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    private string? ToastContainerClass()
    {
        if (_options.Headless) return null;

        var position = _options.ToastPosition switch
        {
            ToastPosition.TopRight => "top-right",
            ToastPosition.TopLeft => "top-left",
            ToastPosition.BottomRight => "bottom-right",
            ToastPosition.BottomLeft => "bottom-left",
            ToastPosition.TopCenter => "top-center",
            ToastPosition.BottomCenter => "bottom-center",
            _ => "top-right"
        };

        return $"flare-toast-container flare-toast-{position}";
    }

    public void Dispose()
    {
        Flare.OnChanged -= HandleChanged;
    }
}
