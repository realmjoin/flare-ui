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
    private int _disposed;

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
            IsActive = true;
            onActivate(this);
        }
        catch (TaskCanceledException)
        {
        }
    }

    /// <summary>
    /// Stops the loading bar. If the bar has not yet appeared (still within the delay), it is cancelled silently.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _onComplete(this);
    }
}
