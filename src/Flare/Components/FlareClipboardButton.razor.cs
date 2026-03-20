using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Flare;

/// <summary>
/// Copies a string to the system clipboard using the browser Clipboard API
/// and shows success/error feedback via Flare toast.
/// </summary>
public partial class FlareClipboardButton : ComponentBase, IAsyncDisposable
{
    [Inject] private IFlareService Flare { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// The text value to copy to the clipboard.
    /// </summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>
    /// Optional custom button content. When <c>null</c>, renders "Copy to clipboard".
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Fires after a copy attempt. The boolean argument is <c>true</c> on success, <c>false</c> on failure.
    /// </summary>
    [Parameter] public EventCallback<bool> OnCopied { get; set; }

    /// <summary>
    /// Additional HTML attributes splatted onto the underlying <c>&lt;button&gt;</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

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
            await module.InvokeVoidAsync("copyToClipboard", Value ?? string.Empty);
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

            await OnCopied.InvokeAsync(success);
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
