using Flare.Internal;
using Microsoft.AspNetCore.Components;

namespace Flare;

/// <summary>
/// Displays a UTC timestamp as a classic relative time string (e.g. "5 minutes ago", "in 2 hours").
/// Formatting and live updates run entirely in the browser via JS — zero SignalR overhead.
/// Shows the absolute timestamp as a tooltip.
/// </summary>
public partial class FlareRelativeTime : ComponentBase
{
    [Inject] private FlareLocaleProvider Locale { get; set; } = default!;
    [Inject] private FlareOptions Options { get; set; } = default!;
    [Inject] private IFlareTimezoneService Timezone { get; set; } = default!;

    /// <summary>
    /// The UTC timestamp to display relative to now. When null or <c>default</c>, the <see cref="Placeholder"/> is rendered instead
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

    // ⚠ Thresholds and rounding must stay in sync with formatRelativeTime() in flare-time.js
    private string FormatFallback()
    {
        var l = Locale.Get("time");
        var diff = DateTimeOffset.UtcNow - Value!.Value;
        var isFuture = diff < TimeSpan.Zero;
        var abs = isFuture ? -diff : diff;

        var text = abs.TotalSeconds < 15 ? l["justNow"]
            : abs.TotalSeconds < 60 ? Plural((int)abs.TotalSeconds, l["second"], l["seconds"])
            : abs.TotalMinutes < 60 ? Plural((int)abs.TotalMinutes, l["minute"], l["minutes"])
            : abs.TotalHours < 24 ? Plural((int)abs.TotalHours, l["hour"], l["hours"])
            : abs.TotalDays < 30 ? Plural((int)abs.TotalDays, l["day"], l["days"])
            : abs.TotalDays < 365 ? Plural((int)(abs.TotalDays / 30), l["month"], l["months"])
            : Plural((int)(abs.TotalDays / 365), l["year"], l["years"]);

        if (text == l["justNow"]) return text;
        var fmt = isFuture ? l["inFormat"] : l["agoFormat"];
        return fmt.Replace("{0}", text);
    }

    private static string Plural(int count, string singular, string plural) =>
        $"{count} {(count == 1 ? singular : plural)}";

    private string FormatTitle() => Timezone.ToClientTime(Value!.Value).ToString(Options.RelativeTimeTitleFormat);
}
