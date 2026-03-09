using Flare.Internal;
using Microsoft.AspNetCore.Components;

namespace Flare;

public partial class FlareToast : ComponentBase, IDisposable
{
    [Parameter, EditorRequired] public ToastInstance Instance { get; set; } = default!;
    [Parameter] public bool Headless { get; set; }
    [Parameter] public EventCallback OnDismiss { get; set; }

    private CancellationTokenSource? _cts;
    private int _remainingMs;
    private long _timerStartedAt;
    private bool _disposed;

    protected override void OnInitialized()
    {
        _remainingMs = Instance.Options.DurationMs;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !Instance.Options.Persistent)
            await StartTimer();
    }

    private async Task StartTimer()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _timerStartedAt = Environment.TickCount64;

        try
        {
            await Task.Delay(_remainingMs, token);
            if (_disposed) return;

            Instance.IsExiting = true;
            await InvokeAsync(StateHasChanged);

            await Task.Delay(300, token);
            if (!_disposed)
                await OnDismiss.InvokeAsync();
        }
        catch (OperationCanceledException)
        {
            // Timer was paused or component disposed
        }
    }

    private void PauseTimer()
    {
        if (Instance.Options.Persistent) return;
        if (!Instance.Options.PauseOnHover || Instance.IsPaused) return;

        Instance.IsPaused = true;
        var elapsed = (int)(Environment.TickCount64 - _timerStartedAt);
        _remainingMs = Math.Max(_remainingMs - elapsed, 100);
        _cts?.Cancel();
        StateHasChanged();
    }

    private async Task ResumeTimer()
    {
        if (Instance.Options.Persistent) return;
        if (!Instance.IsPaused) return;

        Instance.IsPaused = false;
        await StartTimer();
    }

    private async Task HandleClick()
    {
        if (Instance.Options.OnClick is { } onClick)
        {
            await onClick();
            await Dismiss();
        }
    }

    private async Task Dismiss()
    {
        if (_disposed) return;
        _cts?.Cancel();
        Instance.IsExiting = true;
        StateHasChanged();

        try
        {
            await Task.Delay(300);
            if (!_disposed)
                await OnDismiss.InvokeAsync();
        }
        catch (OperationCanceledException) { }
    }

    private string? CssClass()
    {
        if (Headless) return null;

        var level = Instance.Options.Level.ToString().ToLowerInvariant();
        var exiting = Instance.IsExiting ? " flare-toast-exiting" : "";
        return $"flare-toast flare-toast-{level}{exiting}";
    }

    public void Dispose()
    {
        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
