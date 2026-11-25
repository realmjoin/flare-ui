using Flare.Internal;
using Microsoft.AspNetCore.Components;

namespace Flare;

public partial class FlareToast : ComponentBase, IDisposable
{
    [Inject] private FlareLocaleProvider Locale { get; set; } = default!;

    [Parameter, EditorRequired] public ToastInstance Instance { get; set; } = default!;
    [Parameter] public bool Headless { get; set; }
    [Parameter] public EventCallback OnDismiss { get; set; }

    private CancellationTokenSource? _cts;
    private int _remainingMs;
    private long _timerStartedAt;
    private int _pauseEpoch;
    private bool _disposed;
    private bool _hovered;
    private bool _focused;

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

    private Task HandleMouseEnter()
    {
        _hovered = Instance.Options.PauseOnHover;
        return UpdatePause();
    }

    private Task HandleMouseLeave()
    {
        _hovered = false;
        return UpdatePause();
    }

    // Keyboard focus inside the toast always holds the timer, independent of PauseOnHover:
    // otherwise the toast can vanish while someone is tabbing to its buttons.
    private Task HandleFocusIn()
    {
        _focused = true;
        return UpdatePause();
    }

    private Task HandleFocusOut()
    {
        _focused = false;
        return UpdatePause();
    }

    // Hover and focus are tracked separately so that leaving with the mouse does not
    // resume the timer while focus is still inside (and vice versa).
    private async Task UpdatePause()
    {
        if (_hovered || _focused)
            PauseTimer();
        else
            await ResumeTimer();
    }

    private void PauseTimer()
    {
        if (Instance.Options.Persistent || Instance.IsPaused || Instance.IsExiting) return;

        Instance.IsPaused = true;
        var elapsed = (int)(Environment.TickCount64 - _timerStartedAt);
        _remainingMs = Math.Max(_remainingMs - elapsed, 100);
        _pauseEpoch++;
        _cts?.Cancel();
        StateHasChanged();
    }

    private async Task ResumeTimer()
    {
        if (Instance.Options.Persistent) return;
        if (!Instance.IsPaused) return;

        Instance.IsPaused = false;
        _pauseEpoch++;
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
