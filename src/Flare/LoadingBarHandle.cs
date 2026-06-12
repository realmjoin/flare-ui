namespace Flare;

/// <summary>
/// Controls an active loading bar. Dispose this handle to stop the loading bar.
/// The bar only becomes visible after the configured delay, preventing flicker for fast operations.
/// </summary>
/// <example>
/// <code>
/// using var loading = Flare.StartLoadingBar();
/// await Http.GetAsync("/api/data");
/// // Loading bar is automatically hidden when 'loading' is disposed.
/// </code>
/// </example>
public sealed class LoadingBarHandle : IDisposable
{
    private readonly Action<LoadingBarHandle> _onComplete;
    private readonly CancellationTokenSource? _delayCts;
    private readonly Lock _sync = new();
    private bool _disposed;

    internal bool IsActive { get; private set; }

    internal LoadingBarHandle(Action<LoadingBarHandle> onActivate, Action<LoadingBarHandle> onComplete, int delayMs)
    {
        _onComplete = onComplete;

        if (delayMs <= 0)
        {
            IsActive = true;
            onActivate(this);
        }
        else
        {
            _delayCts = new CancellationTokenSource();
            _ = DelayThenActivateAsync(onActivate, delayMs, _delayCts.Token);
        }
    }

    private async Task DelayThenActivateAsync(Action<LoadingBarHandle> onActivate, int delayMs, CancellationToken ct)
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
        // delay can never leave the bar activated without a matching completion.
        lock (_sync)
        {
            if (_disposed) return;
            IsActive = true;
            onActivate(this);
        }
    }

    /// <summary>
    /// Stops the loading bar. If the bar has not yet appeared (still within the delay), it is cancelled silently.
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
