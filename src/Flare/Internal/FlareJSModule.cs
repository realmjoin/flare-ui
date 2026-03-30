using System.Runtime.CompilerServices;
using Microsoft.JSInterop;

namespace Flare.Internal;

internal static class FlareJSModule
{
    private static readonly ConditionalWeakTable<IJSRuntime, Task<IJSObjectReference>> Cache = new();
    private static readonly ConditionalWeakTable<IJSRuntime, Task<IJSObjectReference>> LocaleCache = new();
    private static readonly ConditionalWeakTable<IJSRuntime, Task<IJSObjectReference>> TimeCache = new();
    private static readonly ConditionalWeakTable<IJSRuntime, Task<IJSObjectReference>> TooltipCache = new();
    private static readonly ConditionalWeakTable<IJSRuntime, Task<IJSObjectReference>> TypeaheadCache = new();

    internal static Task<IJSObjectReference> GetFlareModuleAsync(this IJSRuntime js)
    {
        return Cache.GetValue(js, static rt =>
            rt.InvokeAsync<IJSObjectReference>("import", "./_content/Flare.UI/flare.js").AsTask());
    }

    internal static Task<IJSObjectReference> GetFlareLocaleModuleAsync(this IJSRuntime js)
    {
        return LocaleCache.GetValue(js, static rt =>
            rt.InvokeAsync<IJSObjectReference>("import", "./_content/Flare.UI/flare-locale.js").AsTask());
    }

    internal static Task<IJSObjectReference> GetFlareTimeModuleAsync(this IJSRuntime js)
    {
        return TimeCache.GetValue(js, static rt =>
            rt.InvokeAsync<IJSObjectReference>("import", "./_content/Flare.UI/flare-time.js").AsTask());
    }

    internal static Task<IJSObjectReference> GetFlareTooltipModuleAsync(this IJSRuntime js)
    {
        return TooltipCache.GetValue(js, static rt =>
            rt.InvokeAsync<IJSObjectReference>("import", "./_content/Flare.UI/flare-tooltip.js").AsTask());
    }

    internal static Task<IJSObjectReference> GetFlareTypeaheadModuleAsync(this IJSRuntime js)
    {
        return TypeaheadCache.GetValue(js, static rt =>
            rt.InvokeAsync<IJSObjectReference>("import", "./_content/Flare.UI/flare-typeahead.js").AsTask());
    }
}
