using Flare.Internal;

namespace Flare;

public sealed class LoadingToastHandle : IDisposable
{
    private readonly Action<LoadingToastHandle> _onActivate;
    private readonly Action<LoadingToastHandle> _onComplete;
    private readonly Action _notifyChanged;
    private readonly CancellationTokenSource? _delayCts;
    private int _disposed;

    internal LoadingToastState State { get; }
    internal bool IsActive { get; private set; }

    internal LoadingToastHandle(
        string message,
        Action<LoadingToastHandle> onActivate,
        Action<LoadingToastHandle> onComplete,
        Action notifyChanged,
        int delayMs)
    {
        State = new LoadingToastState { Message = message };
        _onActivate = onActivate;
        _onComplete = onComplete;
        _notifyChanged = notifyChanged;

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

    public void Update(string? message = null, int? progress = null)
    {
        if (!IsActive) return;

        if (message is not null)
            State.Message = message;

        State.Progress = progress;
        _notifyChanged();
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
