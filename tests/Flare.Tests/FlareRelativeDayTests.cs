using Xunit;

namespace Flare.Tests;

public class FlareRelativeDayTests
{
    private static readonly Dictionary<string, string> L = new()
    {
        ["earlierToday"] = "earlier today",
        ["earlierTonight"] = "earlier tonight",
        ["today"] = "today",
        ["tomorrow"] = "tomorrow",
        ["yesterday"] = "yesterday",
        ["lastNight"] = "last night",
        ["inDaysFormat"] = "in {0} days",
        ["daysAgoFormat"] = "{0} days ago",
        ["tonight"] = "tonight",
        ["tomorrowNight"] = "tomorrow night",
        ["nightFormat"] = "{0} at night",
    };

    private static string Format(DateTimeOffset value, DateTimeOffset now) =>
        FlareRelativeDay.FormatRelativeDay(value, now, L);

    private static DateTimeOffset D(int year, int month, int day, int hour, int minute = 0) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);

    // ── Future daytime ───────────────────────────────────────────────

    [Fact]
    public void Future_same_day_daytime() =>
        Assert.Equal("today", Format(D(2026, 6, 15, 16), D(2026, 6, 15, 14)));

    [Fact]
    public void Future_tomorrow_daytime() =>
        Assert.Equal("tomorrow", Format(D(2026, 6, 16, 12), D(2026, 6, 15, 14)));

    [Theory]
    [InlineData(2, "in 2 days")]
    [InlineData(5, "in 5 days")]
    [InlineData(30, "in 30 days")]
    [InlineData(365, "in 365 days")]
    public void Future_N_days_daytime(int days, string expected)
    {
        var now = D(2026, 6, 15, 14);
        var value = new DateTimeOffset(now.AddDays(days).Date.AddHours(12), TimeSpan.Zero);
        Assert.Equal(expected, Format(value, now));
    }

    // ── Future nighttime ─────────────────────────────────────────────

    [Fact]
    public void Future_tonight() =>
        Assert.Equal("tonight", Format(D(2026, 6, 15, 23), D(2026, 6, 15, 14)));

    [Fact]
    public void Future_tomorrow_night() =>
        Assert.Equal("tomorrow night", Format(D(2026, 6, 16, 23), D(2026, 6, 15, 14)));

    [Theory]
    [InlineData(2026, 6, 15, 23, "tonight")]
    [InlineData(2026, 6, 16, 0, "tonight")]
    [InlineData(2026, 6, 16, 3, "tonight")]
    [InlineData(2026, 6, 16, 12, "tomorrow")]
    [InlineData(2026, 6, 16, 23, "tomorrow night")]
    [InlineData(2026, 6, 18, 3, "in 2 days at night")]
    public void Daytime_scheduling_matrix(int year, int month, int day, int hour, string expected)
    {
        var now = D(2026, 6, 15, 14);
        Assert.Equal(expected, Format(D(year, month, day, hour), now));
    }

    [Theory]
    [InlineData(2, "in 2 days at night")]
    [InlineData(5, "in 5 days at night")]
    public void Future_N_days_night(int days, string expected)
    {
        var now = D(2026, 6, 15, 14);
        var value = new DateTimeOffset(now.AddDays(days).Date.AddHours(23), TimeSpan.Zero);
        Assert.Equal(expected, Format(value, now));
    }

    // ── Past daytime ─────────────────────────────────────────────────

    [Fact]
    public void Past_earlier_today() =>
        Assert.Equal("earlier today", Format(D(2026, 6, 15, 10), D(2026, 6, 15, 14)));

    [Fact]
    public void Past_yesterday_daytime() =>
        Assert.Equal("yesterday", Format(D(2026, 6, 14, 12), D(2026, 6, 15, 14)));

    [Theory]
    [InlineData(2, "2 days ago")]
    [InlineData(5, "5 days ago")]
    [InlineData(30, "30 days ago")]
    [InlineData(365, "365 days ago")]
    public void Past_N_days_daytime(int days, string expected)
    {
        var now = D(2026, 6, 15, 14);
        var value = new DateTimeOffset(now.AddDays(-days).Date.AddHours(12), TimeSpan.Zero);
        Assert.Equal(expected, Format(value, now));
    }

    // ── Past nighttime ───────────────────────────────────────────────

    [Fact]
    public void Past_early_morning_from_daytime_is_last_night() =>
        // It's 2pm, 3am today belongs to the previous night → "last night"
        Assert.Equal("last night", Format(D(2026, 6, 15, 3), D(2026, 6, 15, 14)));

    [Fact]
    public void Past_earlier_tonight_when_now_is_night() =>
        // It's 4am, 1am today already happened → "earlier tonight" (both in same night)
        Assert.Equal("earlier tonight", Format(D(2026, 6, 15, 1), D(2026, 6, 15, 4)));

    [Fact]
    public void Past_last_night() =>
        Assert.Equal("last night", Format(D(2026, 6, 14, 23), D(2026, 6, 15, 14)));

    [Theory]
    [InlineData(2026, 6, 15, 10, "earlier today")]
    [InlineData(2026, 6, 15, 3, "last night")]
    [InlineData(2026, 6, 14, 23, "last night")]
    [InlineData(2026, 6, 14, 3, "2 days ago at night")]
    [InlineData(2026, 6, 14, 12, "yesterday")]
    public void Daytime_history_matrix(int year, int month, int day, int hour, string expected)
    {
        var now = D(2026, 6, 15, 14);
        Assert.Equal(expected, Format(D(year, month, day, hour), now));
    }

    [Theory]
    [InlineData(2, "3 days ago at night")]
    [InlineData(5, "6 days ago at night")]
    [InlineData(100, "101 days ago at night")]
    public void Past_N_days_night(int days, string expected)
    {
        var now = D(2026, 6, 15, 14);
        var value = new DateTimeOffset(now.AddDays(-days).Date.AddHours(1), TimeSpan.Zero);
        Assert.Equal(expected, Format(value, now));
    }

    // ── IsNight boundary: 23 is night, 22 is day ────────────────────

    [Theory]
    [InlineData(0, true)]   // midnight
    [InlineData(5, true)]   // last night hour
    [InlineData(6, false)]  // first day hour
    [InlineData(12, false)] // noon
    [InlineData(22, false)] // still daytime
    [InlineData(23, true)]  // first night hour
    public void IsNight_boundaries(int hour, bool expected) =>
        Assert.Equal(expected, FlareRelativeDay.IsNight(hour));

    // ── Hour 22 is daytime ───────────────────────────────────────────

    [Fact]
    public void Hour_22_future_is_today_not_tonight() =>
        Assert.Equal("today", Format(D(2026, 6, 15, 22), D(2026, 6, 15, 14)));

    [Fact]
    public void Hour_22_past_is_earlier_today() =>
        Assert.Equal("earlier today", Format(D(2026, 6, 15, 22), D(2026, 6, 15, 22, 30)));

    // ── Same-night collapse (midnight crossing) ──────────────────────
    //
    // 23:00 day X through 05:59 day X+1 is one continuous night.
    // When both now and value are in that window, dayDiff collapses to 0.

    [Fact]
    public void SameNight_23_to_3am_next_day_is_tonight()
    {
        // It's 23:00, event at 03:00 next calendar day → same night → "tonight"
        Assert.Equal("tonight", Format(D(2026, 6, 16, 3), D(2026, 6, 15, 23)));
    }

    [Fact]
    public void SameNight_3am_to_23_previous_day_is_earlier_tonight()
    {
        // It's 03:00, event was at 23:00 previous calendar day → same night, past
        Assert.Equal("earlier tonight", Format(D(2026, 6, 15, 23), D(2026, 6, 16, 3)));
    }

    [Fact]
    public void SameNight_midnight_to_23_previous_day()
    {
        // It's midnight, event was at 23:00 yesterday → same night, past
        Assert.Equal("earlier tonight", Format(D(2026, 6, 15, 23), D(2026, 6, 16, 0)));
    }

    [Fact]
    public void SameNight_23_to_midnight_next_day()
    {
        // It's 23:00, event at 00:00 next day → same night, future
        Assert.Equal("tonight", Format(D(2026, 6, 16, 0), D(2026, 6, 15, 23)));
    }

    [Fact]
    public void SameNight_2359_to_0000_next_day()
    {
        // 23:59 → 00:00, 1 minute apart across calendar boundary → same night
        Assert.Equal("tonight", Format(D(2026, 6, 16, 0), D(2026, 6, 15, 23, 59)));
    }

    // ── NOT same night (both night, but separate nights) ─────────────

    [Fact]
    public void Not_sameNight_both_early_morning_adjacent_days()
    {
        // 03:00 and 03:00 next day — different nights
        Assert.Equal("tomorrow night", Format(D(2026, 6, 16, 3), D(2026, 6, 15, 3)));
    }

    [Fact]
    public void Not_sameNight_both_late_evening_adjacent_days()
    {
        // 23:00 and 23:00 next day — different nights
        Assert.Equal("tomorrow night", Format(D(2026, 6, 16, 23), D(2026, 6, 15, 23)));
    }

    [Fact]
    public void Not_sameNight_two_calendar_days_apart()
    {
        // 23:00 day X and 03:00 day X+2 land in consecutive future night labels: tonight, then tomorrow night.
        Assert.Equal("tomorrow night", Format(D(2026, 6, 17, 3), D(2026, 6, 15, 23)));
    }

    // ── Now at nighttime, value at daytime ───────────────────────────

    [Fact]
    public void Now_23_value_noon_same_day_is_earlier_today()
    {
        // 23:00, looking back at noon today → "earlier today"
        Assert.Equal("earlier today", Format(D(2026, 6, 15, 12), D(2026, 6, 15, 23)));
    }

    [Fact]
    public void Now_23_value_noon_next_day_is_tomorrow()
    {
        Assert.Equal("tomorrow", Format(D(2026, 6, 16, 12), D(2026, 6, 15, 23)));
    }

    [Fact]
    public void Now_3am_value_noon_same_day_is_today()
    {
        // 03:00, noon today is still ahead → "today"
        Assert.Equal("today", Format(D(2026, 6, 15, 12), D(2026, 6, 15, 3)));
    }

    [Fact]
    public void Now_3am_value_14_previous_day_is_yesterday()
    {
        Assert.Equal("yesterday", Format(D(2026, 6, 15, 14), D(2026, 6, 16, 3)));
    }

    [Theory]
    [InlineData(2026, 6, 15, 23, "earlier tonight")]
    [InlineData(2026, 6, 16, 1, "earlier tonight")]
    [InlineData(2026, 6, 16, 5, "tonight")]
    [InlineData(2026, 6, 16, 12, "today")]
    [InlineData(2026, 6, 17, 1, "tomorrow night")]
    [InlineData(2026, 6, 14, 23, "last night")]
    public void Nighttime_matrix_from_early_morning_now(int year, int month, int day, int hour, string expected)
    {
        var now = D(2026, 6, 16, 3);
        Assert.Equal(expected, Format(D(year, month, day, hour), now));
    }

    // ── Now at day/night boundary ────────────────────────────────────

    [Fact]
    public void Now_0600_value_23_previous_day_is_last_night()
    {
        // now=06:00 (first daytime hour) → previous night's 23:00 is "last night"
        Assert.Equal("last night", Format(D(2026, 6, 15, 23), D(2026, 6, 16, 6)));
    }

    [Fact]
    public void Now_2300_value_5am_next_day_is_tonight()
    {
        // now=23:00, value=05:00 next day → same night → "tonight"
        Assert.Equal("tonight", Format(D(2026, 6, 16, 5), D(2026, 6, 15, 23)));
    }

    [Fact]
    public void Now_2300_value_0600_next_day_is_tomorrow()
    {
        // now=23:00, value=06:00 next day → value is daytime → "tomorrow"
        Assert.Equal("tomorrow", Format(D(2026, 6, 16, 6), D(2026, 6, 15, 23)));
    }

    // ── Calendar boundary crossings ──────────────────────────────────

    [Fact]
    public void Year_boundary_forward() =>
        Assert.Equal("tomorrow", Format(D(2027, 1, 1, 12), D(2026, 12, 31, 14)));

    [Fact]
    public void Year_boundary_backward() =>
        Assert.Equal("yesterday", Format(D(2026, 12, 31, 12), D(2027, 1, 1, 14)));

    [Fact]
    public void SameNight_across_year_boundary()
    {
        // Dec 31 23:00 → Jan 1 03:00 → same night
        Assert.Equal("tonight", Format(D(2027, 1, 1, 3), D(2026, 12, 31, 23)));
    }

    [Fact]
    public void Feb28_to_mar1_non_leap() =>
        // 2027 is not a leap year
        Assert.Equal("tomorrow", Format(D(2027, 3, 1, 12), D(2027, 2, 28, 14)));

    [Fact]
    public void Feb28_to_mar1_leap_year() =>
        // 2028 is a leap year — 2 days apart
        Assert.Equal("in 2 days", Format(D(2028, 3, 1, 12), D(2028, 2, 28, 14)));

    // ── Midnight edge cases ──────────────────────────────────────────

    [Fact]
    public void Midnight_next_day_from_daytime_is_tonight()
    {
        // 14:00 → 00:00 next day is the upcoming night, which people call "tonight"
        Assert.Equal("tonight", Format(D(2026, 6, 16, 0), D(2026, 6, 15, 14)));
    }

    [Fact]
    public void Early_morning_next_day_from_daytime_is_tonight()
    {
        // 14:00 → 03:00 next day is still the upcoming night → "tonight"
        Assert.Equal("tonight", Format(D(2026, 6, 16, 3), D(2026, 6, 15, 14)));
    }

    [Fact]
    public void Value_2259_is_daytime() =>
        Assert.Equal("today", Format(D(2026, 6, 15, 22, 59), D(2026, 6, 15, 14)));

    [Fact]
    public void Value_2300_is_nighttime() =>
        Assert.Equal("tonight", Format(D(2026, 6, 15, 23), D(2026, 6, 15, 14)));

    // ── dayDiff ±2 boundary ──────────────────────────────────────────

    [Fact]
    public void Two_days_future_not_tomorrow() =>
        Assert.Equal("in 2 days", Format(D(2026, 6, 17, 12), D(2026, 6, 15, 14)));

    [Fact]
    public void Two_days_past_not_yesterday() =>
        Assert.Equal("2 days ago", Format(D(2026, 6, 13, 12), D(2026, 6, 15, 14)));
}
