namespace Flare;

public sealed class ModalContext
{
    private readonly Action<ModalResult> _close;

    internal ModalContext(Action<ModalResult> close) => _close = close;

    public void Close(ModalResult result) => _close(result);
    public void Ok(object? data = null) => _close(ModalResult.Ok(data));
    public void Cancel() => _close(ModalResult.Cancel());
}
