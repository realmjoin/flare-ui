using System.Runtime.CompilerServices;
using Microsoft.JSInterop;

namespace Flare.Internal;

internal static class FlareJSModule
{
    private static readonly ConditionalWeakTable<IJSRuntime, Task<IJSObjectReference>> Cache = new();

    internal static Task<IJSObjectReference> GetFlareModuleAsync(this IJSRuntime js)
    {
        return Cache.GetValue(js, static rt =>
            rt.InvokeAsync<IJSObjectReference>("import", "./_content/Flare.UI/flare.js").AsTask());
    }
}
