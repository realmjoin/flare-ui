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
    private readonly Lock _sync = new();
    private bool _disposed;

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
        }
        catch (TaskCanceledException)
        {
            return;
        }

        // Activation and disposal are mutually exclusive so a Dispose racing the end of the
        // delay can never leave the toast activated without a matching completion.
        lock (_sync)
        {
            if (_disposed) return;
            IsActive = true;
            _onActivate(this);
        }
    }

    /// <summary>
    /// Dismisses the loading toast. If the toast has not yet appeared (still within the delay), it is cancelled silently.
    /// </summary>
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
        }

        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _onComplete(this);
    }
}
