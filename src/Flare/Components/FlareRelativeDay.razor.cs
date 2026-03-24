using Flare.Internal;
using Microsoft.AspNetCore.Components;

namespace Flare;

/// <summary>
/// Displays a UTC timestamp relative to the current day, focused on day granularity and time of day.
/// Ideal for release planning (e.g. "tomorrow, at night", "in 3 days", "2 days ago").
/// Night is defined as 22:00–05:59 in the client's local timezone.
/// Formatting and live updates run entirely in the browser via JS — zero SignalR overhead.
/// </summary>
public partial class FlareRelativeDay : ComponentBase
{
    [Inject] private FlareLocaleProvider Locale { get; set; } = default!;

    /// <summary>
    /// The UTC timestamp to display.
    /// </summary>
    [Parameter, EditorRequired] public DateTimeOffset Value { get; set; }

    /// <summary>
    /// Additional HTML attributes splatted onto the wrapping <c>&lt;span&gt;</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

    // ⚠ Logic must stay in sync with formatRelativeDay() in flare-time.js
    private string FormatFallback()
    {
        var l = Locale.Get("time");
        var now = DateTimeOffset.UtcNow;
        var dayDiff = (Value.Date - now.Date).Days;
        var hour = Value.Hour;
        var isNight = hour >= 22 || hour < 6;

        var dayPart = dayDiff switch
        {
            0 => l["today"],
            1 => l["tomorrow"],
            -1 => l["yesterday"],
            > 1 => l["inDaysFormat"].Replace("{0}", dayDiff.ToString()),
            _ => l["daysAgoFormat"].Replace("{0}", (-dayDiff).ToString()),
        };

        return isNight ? l["nightFormat"].Replace("{0}", dayPart) : dayPart;
    }
}
