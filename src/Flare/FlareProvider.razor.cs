using Flare.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Flare;

/// <summary>
/// Root component that renders toasts, modals, confirm dialogs, and loading indicators.
/// Wrap your page or layout content with this component.
/// </summary>
/// <example>
/// <code>
/// &lt;FlareProvider&gt;
///     @* your content *@
/// &lt;/FlareProvider&gt;
/// </code>
/// </example>
public partial class FlareProvider : ComponentBase, IDisposable
{
    [Inject] private IFlareService Flare { get; set; } = default!;
    [Inject] private FlareOptions _options { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IFlareTimezoneService Timezone { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// The application content to render inside the provider.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private FlareService _service => (FlareService)Flare;

    protected override void OnInitialized()
    {
        Flare.OnChanged += HandleChanged;
        Navigation.LocationChanged += HandleLocationChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try { await JS.GetFlareModuleAsync(); }
            catch { }

            await ((FlareTimezoneService)Timezone).InitializeAsync(JS);

            var localeModule = await JS.GetFlareLocaleModuleAsync();
            await localeModule.InvokeVoidAsync("init", _options.Locale, _options.Debug);

            var timeModule = await JS.GetFlareTimeModuleAsync();
            await timeModule.InvokeVoidAsync("init");

            var tooltipModule = await JS.GetFlareTooltipModuleAsync();
            await tooltipModule.InvokeVoidAsync("init");

            StateHasChanged();
        }
    }

    private void HandleChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (_service.ActiveModal is { Options.CloseOnNavigation: true })
            _service.CloseModal(ModalResult.Cancel());

        if (_service.ActiveConfirm is { Options.CloseOnNavigation: true })
            _service.CloseConfirm(false);
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

    /// <inheritdoc />
    public void Dispose()
    {
        Flare.OnChanged -= HandleChanged;
        Navigation.LocationChanged -= HandleLocationChanged;
    }
}
