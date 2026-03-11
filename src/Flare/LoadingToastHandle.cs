using Flare.Internal;

namespace Flare;

/// <summary>
/// Controls an active loading toast. Use <see cref="Update"/> to change the message or report progress,
/// and dispose the handle to dismiss the toast.
/// </summary>
/// <example>
/// <code>
/// using var toast = Flare.StartLoadingToast("Uploading…");
/// toast.Update(progress: 50);
/// toast.Update("Almost done…", progress: 90);
/// // Toast is dismissed when 'toast' is disposed.
/// </code>
/// </example>
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

    /// <summary>
    /// Updates the loading toast message and/or progress.
    /// </summary>
    /// <param name="message">New message text. Pass <c>null</c> to keep the current message.</param>
    /// <param name="progress">Progress percentage (0–100). Pass <c>null</c> to show an indeterminate state.</param>
    public void Update(string? message = null, int? progress = null)
    {
        if (message is not null)
            State.Message = message;

        State.Progress = progress;

        if (IsActive)
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

    /// <summary>
    /// Dismisses the loading toast. If the toast has not yet appeared (still within the delay), it is cancelled silently.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _onComplete(this);
    }
}
