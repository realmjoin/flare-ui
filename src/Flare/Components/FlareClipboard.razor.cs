using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Flare;

/// <summary>
/// Renders inline content that copies its text to the clipboard on click.
/// Shows a "Copy" tooltip on hover and uses pointer cursor. Applies no
/// default styling — use <see cref="Class"/> to add custom CSS classes.
/// </summary>
public partial class FlareClipboard : ComponentBase, IAsyncDisposable
{
    [Inject] private IFlareService Flare { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// The content to display. The rendered text of this fragment is also the
    /// value copied to the clipboard unless <see cref="Value"/> is set.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Explicit value to copy. When <c>null</c>, the component copies its own
    /// rendered text content via the DOM.
    /// </summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>
    /// CSS class(es) applied to the wrapping <c>&lt;span&gt;</c> element.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Additional HTML attributes splatted onto the underlying <c>&lt;span&gt;</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

    private ElementReference _element;
    private IJSObjectReference? _module;
    private bool _disposed;

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        return _module ??= await JS.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Flare.UI/flare-clipboard.js");
    }

    private async Task HandleClick()
    {
        bool success;
        try
        {
            var module = await GetModuleAsync();

            var text = Value ?? await module.InvokeAsync<string>("getTextContent", _element);
            await module.InvokeVoidAsync("copyToClipboard", text);
            success = true;
        }
        catch
        {
            success = false;
        }

        if (!_disposed)
        {
            if (success)
                _ = Flare.ToastAsync("Copied to clipboard", ToastLevel.Success);
            else
                _ = Flare.ToastAsync("Failed to copy to clipboard", ToastLevel.Error);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException)
            {
                // Expected during circuit teardown
            }
        }
    }
}
