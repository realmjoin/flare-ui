using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Flare;

/// <summary>
/// A button that prevents double-click and rapid-fire submits by disabling itself
/// for a cooldown period after each click.
/// </summary>
public partial class FlareDebouncedButton : ComponentBase, IDisposable
{
    [Inject] private ILogger<FlareDebouncedButton> Logger { get; set; } = default!;

    /// <summary>
    /// The button content.
    /// </summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    /// Fires when the button is clicked (at most once per cooldown period).
    /// </summary>
    [Parameter, EditorRequired] public EventCallback OnAction { get; set; }

    /// <summary>
    /// Cooldown period after each click before the button re-enables. Defaults to 1 second.
    /// </summary>
    [Parameter] public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Additional HTML attributes splatted onto the underlying <c>&lt;button&gt;</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

    private bool _cooling;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    private bool IsDisabled =>
        _cooling
        || (InputAttributes?.TryGetValue("disabled", out var v) == true && IsTruthy(v));

    private Dictionary<string, object>? Attributes
    {
        get
        {
            if (InputAttributes is null) return null;
            var attrs = new Dictionary<string, object>(InputAttributes);
            attrs.Remove("disabled");
            return attrs.Count > 0 ? attrs : null;
        }
    }

    private async Task HandleClick()
    {
        if (IsDisabled) return;

        _cooling = true;
        StateHasChanged();

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // Fire action on a background thread to avoid blocking the UI
        _ = Task.Run(async () =>
        {
            try
            {
                await OnAction.InvokeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "FlareDebouncedButton action threw an exception");
            }
        });

        try
        {
            await Task.Delay(Timeout, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (!_disposed)
        {
            _cooling = false;
            await InvokeAsync(StateHasChanged);
        }
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
