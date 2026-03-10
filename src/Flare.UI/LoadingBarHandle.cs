namespace Flare;

public sealed class LoadingBarHandle : IDisposable
{
    private readonly Action<LoadingBarHandle> _onActivate;
    private readonly Action<LoadingBarHandle> _onComplete;
    private readonly CancellationTokenSource? _delayCts;
    private int _disposed;

    internal bool IsActive { get; private set; }

    internal LoadingBarHandle(Action<LoadingBarHandle> onActivate, Action<LoadingBarHandle> onComplete, int delayMs)
    {
        _onActivate = onActivate;
        _onComplete = onComplete;

        if (delayMs <= 0)
        {
            IsActive = true;
            _onActivate(this);
        }
        else
        {
            _delayCts = new CancellationTokenSource();
            _ = DelayThenActivateAsync(delayMs, _delayCts.Token);
        }
    }

    private async Task DelayThenActivateAsync(int delayMs, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delayMs, ct);
            IsActive = true;
            _onActivate(this);
        }
        catch (TaskCanceledException)
        {
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _onComplete(this);
    }
}
