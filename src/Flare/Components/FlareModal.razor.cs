using Flare.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Flare;

public partial class FlareModal : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter, EditorRequired] public ModalInstance Instance { get; set; } = default!;
    [Parameter] public bool Headless { get; set; }
    [Parameter] public EventCallback<ModalResult> OnClose { get; set; }

    private ElementReference _backdrop;
    private ElementReference _dialog;
    private IJSObjectReference? _module;
    private IJSObjectReference? _trap;
    private ModalContext _modalContext = default!;
    private readonly string _titleId = $"flare-modal-title-{Guid.NewGuid():N}";

    protected override void OnInitialized()
    {
        _modalContext = new ModalContext(result => Close(result));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Flare.UI/flare.js");
            _trap = await _module.InvokeAsync<IJSObjectReference>(
                "createFocusTrap", _dialog, null);
        }
    }

    private void HandleBackdropClick()
    {
        if (Instance.Options.CloseOnBackdropClick)
            Close(ModalResult.Cancel());
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape" && Instance.Options.CloseOnEscape)
            Close(ModalResult.Cancel());
    }

    private void Close(ModalResult result)
    {
        _ = OnClose.InvokeAsync(result);
    }

    private string? BackdropClass() => Headless ? null : "flare-modal-backdrop";
    private string? DialogClass() => Headless ? null : "flare-modal-dialog";

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
