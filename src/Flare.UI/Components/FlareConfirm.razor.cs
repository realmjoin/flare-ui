using Flare.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Flare;

public partial class FlareConfirm : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter, EditorRequired] public ConfirmInstance Instance { get; set; } = default!;
    [Parameter] public bool Headless { get; set; }
    [Parameter] public EventCallback<bool> OnClose { get; set; }

    private ElementReference _backdrop;
    private ElementReference _dialog;
    private IJSObjectReference? _module;
    private IJSObjectReference? _trap;
    private readonly string _titleId = $"flare-confirm-title-{Guid.NewGuid():N}";
    private readonly string _descId = $"flare-confirm-desc-{Guid.NewGuid():N}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Flare.UI/flare.js");
            _trap = await _module.InvokeAsync<IJSObjectReference>(
                "createFocusTrap", _dialog, ".flare-confirm-cancel");
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

    public async ValueTask DisposeAsync()
    {
        if (_trap is not null && _module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("destroyFocusTrap", _trap);
            }
            catch (JSDisconnectedException) { }
        }

        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
        }
    }
}
