using Microsoft.AspNetCore.Components;

namespace Flare;

/// <summary>
/// A two-stage confirmation button that prevents accidental destructive actions.
/// Supports timed mode (arm → confirm within window) and modal mode (opens a Flare confirm dialog).
/// </summary>
public partial class FlareConfirmButton : ComponentBase, IDisposable
{
    [Inject] private IFlareService Flare { get; set; } = default!;

    /// <summary>
    /// Content displayed in the default (standby) state.
    /// </summary>
    [Parameter, EditorRequired] public RenderFragment Standby { get; set; } = default!;

    /// <summary>
    /// Content displayed in the armed state. Defaults to "Confirm".
    /// </summary>
    [Parameter] public RenderFragment? Armed { get; set; }

    /// <summary>
    /// Fires when the action is confirmed (second click in timed mode, or dialog confirmation in modal mode).
    /// </summary>
    [Parameter] public EventCallback OnConfirmed { get; set; }

    /// <summary>
    /// When <c>true</c>, uses a Flare confirm dialog instead of the timed arm/confirm flow.
    /// </summary>
    [Parameter] public bool UseModal { get; set; }

    /// <summary>
    /// Title for the confirm dialog when <see cref="UseModal"/> is <c>true</c>.
    /// </summary>
    [Parameter] public string ModalHeading { get; set; } = "Confirm";

    /// <summary>
    /// Optional message body for the confirm dialog when <see cref="UseModal"/> is <c>true</c>.
    /// </summary>
    [Parameter] public string? ModalMessage { get; set; }

    /// <summary>
    /// When <c>true</c>, the button permanently disables after confirmation. Defaults to <c>true</c>.
    /// Set to <c>false</c> for repeatable actions like rollback.
    /// </summary>
    [Parameter] public bool DisableOnConfirm { get; set; } = true;

    /// <summary>
    /// CSS class merged into the button when in the armed state.
    /// </summary>
    [Parameter] public string? ArmedClass { get; set; }

    /// <summary>
    /// Delay in milliseconds before the button becomes clickable in the armed state.
    /// </summary>
    [Parameter] public int ArmDelayMs { get; set; } = 2000;

    /// <summary>
    /// Duration in milliseconds the button stays armed before resetting.
    /// </summary>
    [Parameter] public int ArmedWindowMs { get; set; } = 8000;

    /// <summary>
    /// Additional HTML attributes splatted onto the underlying <c>&lt;button&gt;</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

    private ButtonState _state = ButtonState.Standby;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    private enum ButtonState { Standby, ArmDelay, Armed, Confirmed }

    private bool _isArmed => _state is ButtonState.ArmDelay or ButtonState.Armed;

    private bool IsDisabled =>
        _state is ButtonState.ArmDelay or ButtonState.Confirmed
        || (InputAttributes?.TryGetValue("disabled", out var v) == true && IsTruthy(v));

    private Dictionary<string, object>? Attributes
    {
        get
        {
            var attrs = InputAttributes is not null
                ? new Dictionary<string, object>(InputAttributes)
                : new Dictionary<string, object>();

            // Remove disabled — we handle it via IsDisabled
            attrs.Remove("disabled");

            if (_state is ButtonState.ArmDelay or ButtonState.Armed && !string.IsNullOrEmpty(ArmedClass))
            {
                attrs.TryGetValue("class", out var existing);
                var current = existing as string ?? string.Empty;
                attrs["class"] = string.IsNullOrEmpty(current) ? ArmedClass : $"{current} {ArmedClass}";
            }

            return attrs.Count > 0 ? attrs : null;
        }
    }

    private async Task HandleClick()
    {
        if (IsDisabled) return;

        if (UseModal)
        {
            await HandleModalMode();
        }
        else
        {
            await HandleTimedMode();
        }
    }

    private async Task HandleModalMode()
    {
        if (_state == ButtonState.Confirmed) return;

        var confirmed = await Flare.ConfirmAsync(
            ModalHeading,
            ModalMessage ?? string.Empty,
            new ConfirmOptions { Intent = ConfirmIntent.Danger });

        if (confirmed)
        {
            _state = DisableOnConfirm ? ButtonState.Confirmed : ButtonState.Standby;
            await OnConfirmed.InvokeAsync();
        }
    }

    private async Task HandleTimedMode()
    {
        switch (_state)
        {
            case ButtonState.Standby:
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                _state = ButtonState.ArmDelay;
                StateHasChanged();

                try
                {
                    await Task.Delay(ArmDelayMs, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (token.IsCancellationRequested) return;

                _state = ButtonState.Armed;
                StateHasChanged();

                _ = ExpireArmedWindowAsync(token);
                break;

            case ButtonState.Armed:
                _cts?.Cancel();
                _state = DisableOnConfirm ? ButtonState.Confirmed : ButtonState.Standby;
                await OnConfirmed.InvokeAsync();
                break;
        }
    }

    private async Task ExpireArmedWindowAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(ArmedWindowMs, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested || _disposed) return;

        _state = ButtonState.Standby;
        await InvokeAsync(StateHasChanged);
        _ = Flare.ToastAsync("Confirmation expired. Please try again.", ToastLevel.Warning);
    }

    protected override void OnParametersSet()
    {
        Armed ??= builder =>
        {
            builder.AddContent(0, "Confirm");
        };
    }

    public void Dispose()
    {
        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private static bool IsTruthy(object? value) => value switch
    {
        bool b => b,
        string s => s is not ("false" or ""),
        null => false,
        _ => true
    };
}
