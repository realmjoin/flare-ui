using Flare.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Flare;

public partial class FlareConfirm : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private FlareOptions Options { get; set; } = default!;

    [Parameter, EditorRequired] public ConfirmInstance Instance { get; set; } = default!;
    [Parameter] public bool Headless { get; set; }
    [Parameter] public EventCallback<bool> OnClose { get; set; }

    private ElementReference _backdrop;
    private ElementReference _dialog;
    private IJSObjectReference? _trap;
    private readonly string _titleId = $"flare-confirm-title-{Guid.NewGuid():N}";
    private readonly string _descId = $"flare-confirm-desc-{Guid.NewGuid():N}";
    private bool _disposed;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_disposed)
        {
            try
            {
                var module = await JS.GetFlareModuleAsync();
                if (!_disposed)
                {
                    _trap = await module.InvokeAsync<IJSObjectReference>(
                        "createFocusTrap", _dialog, ".flare-confirm-cancel");
                }
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException or JSException) { }
        }
    }

    private void HandleBackdropClick()
    {
        if (Instance.Options.CloseOnBackdropClick)
            Close(false);
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape" && Instance.Options.CloseOnEscape)
            Close(false);
    }

    private void Close(bool confirmed)
    {
        _ = OnClose.InvokeAsync(confirmed);
    }

    private string? BackdropClass() => Headless ? null : "flare-confirm-backdrop";
    private string? DialogClass() => Headless ? null : "flare-confirm-dialog";

    private string ConfirmBtnClass() => Instance.Options.Intent switch
    {
        ConfirmIntent.Danger => "flare-btn-danger",
        _ => "flare-btn-primary",
    };

    private string CancelBtnClass() =>
        Instance.Options.DefaultButton == DefaultButton.Cancel && Instance.Options.Intent != ConfirmIntent.Danger
            ? "flare-btn-primary" : "";

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_trap is not null)
        {
            try
            {
                var module = await JS.GetFlareModuleAsync();
                await module.InvokeVoidAsync("destroyFocusTrap", _trap);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
        }
    }
}
