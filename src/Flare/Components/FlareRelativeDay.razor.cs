using Flare.Internal;
using Microsoft.AspNetCore.Components;

namespace Flare;

/// <summary>
/// Displays a UTC timestamp relative to the current day, focused on day granularity and time of day.
/// Ideal for release planning (e.g. "tonight", "in 3 days", "last night").
/// Night is defined as 23:00–05:59 in the client's local timezone.
/// Formatting and live updates run entirely in the browser via JS — zero SignalR overhead.
/// </summary>
public partial class FlareRelativeDay : ComponentBase
{
    [Inject] private FlareLocaleProvider Locale { get; set; } = default!;
    [Inject] private FlareOptions Options { get; set; } = default!;
    [Inject] private IFlareTimezoneService Timezone { get; set; } = default!;

    /// <summary>
    /// The UTC timestamp to display. When null or <c>default</c>, the <see cref="Placeholder"/> is rendered instead
    /// (or nothing at all if <see cref="Placeholder"/> is empty).
    /// </summary>
    [Parameter, EditorRequired] public DateTimeOffset? Value { get; set; }

    /// <summary>
    /// Text to render when <see cref="Value"/> is null or <c>default</c>. Empty (the default) renders no element at all.
    /// </summary>
    [Parameter] public string Placeholder { get; set; } = "";

    /// <summary>
    /// Additional HTML attributes splatted onto the wrapping <c>&lt;span&gt;</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

    internal bool HasValue => Value is { } v && v != default;

    // ⚠ Logic must stay in sync with formatRelativeDay() in flare-time.js
    private string FormatFallback()
    {
        // Day boundaries and night detection depend on local time — convert when the client timezone is known.
        var value = Timezone.IsInitialized ? Timezone.ToClientTime(Value!.Value) : Value!.Value;
        var now = Timezone.IsInitialized ? Timezone.ToClientTime(DateTimeOffset.UtcNow) : DateTimeOffset.UtcNow;
        return FormatRelativeDay(value, now, Locale.Get("time"));
    }

    internal static bool IsNight(int hour) => hour >= 23 || hour < 6;

    internal static DateTime GetNightDay(DateTimeOffset value) =>
        value.Hour < 6 ? value.Date.AddDays(-1) : value.Date;

    private string FormatTitle() => Timezone.ToClientTime(Value!.Value).ToString(Options.RelativeTimeTitleFormat);

    internal static string FormatRelativeDay(DateTimeOffset value, DateTimeOffset now, IReadOnlyDictionary<string, string> l)
    {
        var dayDiff = (value.Date - now.Date).Days;
        var isPast = value < now;
        var isNight = IsNight(value.Hour);
        var isNowNight = IsNight(now.Hour);

        if (isNight)
        {
            var referenceNightDay = isNowNight ? GetNightDay(now) : now.Date;
            var nightDiff = (GetNightDay(value) - referenceNightDay).Days;

            return (nightDiff, isPast) switch
            {
                (0, true) => isNowNight ? l["earlierTonight"] : l["tonight"],
                (0, false) => l["tonight"],
                (1, _) => l["tomorrowNight"],
                (-1, _) => l["lastNight"],
                (> 1, _) => l["nightFormat"].Replace("{0}", l["inDaysFormat"].Replace("{0}", nightDiff.ToString())),
                (_, _) => l["nightFormat"].Replace("{0}", l["daysAgoFormat"].Replace("{0}", (-nightDiff).ToString())),
            };
        }

        return (dayDiff, isPast) switch
        {
            (0, true) => l["earlierToday"],
            (0, false) => l["today"],
            (1, _) => l["tomorrow"],
            (-1, _) => l["yesterday"],
            (> 1, _) => l["inDaysFormat"].Replace("{0}", dayDiff.ToString()),
            (_, _) => l["daysAgoFormat"].Replace("{0}", (-dayDiff).ToString()),
        };
    }
}
