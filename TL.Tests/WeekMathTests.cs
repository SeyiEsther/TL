using TL.Pages;
using TL.Services;

namespace TL.Tests;

public class WeekMathTests
{
    [Theory]
    // Any day in the week resolves to the same Monday–Sunday bounds.
    [InlineData("2026-07-22", "2026-07-20", "2026-07-26")] // Wed -> Mon..Sun
    [InlineData("2026-07-20", "2026-07-20", "2026-07-26")] // Mon (start)
    [InlineData("2026-07-26", "2026-07-20", "2026-07-26")] // Sun (end)
    public void Bounds_are_monday_to_sunday(string input, string start, string end)
    {
        var (s, e) = WeekMath.Bounds(DateOnly.Parse(input));
        Assert.Equal(DateOnly.Parse(start), s);
        Assert.Equal(DateOnly.Parse(end), e);
    }

    [Fact]
    public void Tuesday_and_thursday_same_week_share_bounds_but_next_monday_differs()
    {
        var tue = WeekMath.Bounds(new DateOnly(2026, 7, 21)).Start;
        var thu = WeekMath.Bounds(new DateOnly(2026, 7, 23)).Start;
        var nextMon = WeekMath.Bounds(new DateOnly(2026, 7, 27)).Start;
        Assert.Equal(tue, thu);
        Assert.NotEqual(tue, nextMon);
    }

    [Fact]
    public void Label_includes_iso_week_number()
    {
        // 2026-07-20 is the Monday of ISO week 30.
        Assert.Equal(30, WeekMath.IsoWeekNumber(new DateOnly(2026, 7, 20)));
        Assert.StartsWith("Week 30", WeekMath.Label(new DateOnly(2026, 7, 20)));
    }

    [Theory]
    [InlineData(35, false)] // exactly on target is NOT under
    [InlineData(34, true)]  // one short is under
    [InlineData(0, true)]
    [InlineData(50, false)]
    public void Shift_row_flags_under_target_below_35(int achieved, bool expectedUnder)
    {
        var row = new ShiftTargetRow(new DateOnly(2026, 7, 20), "Day", achieved);
        Assert.Equal(35, row.Target);
        Assert.Equal(expectedUnder, row.Under);
    }

    [Fact]
    public void Shift_row_progress_caps_at_100_percent()
    {
        Assert.Equal(100, new ShiftTargetRow(new DateOnly(2026, 7, 20), "Day", 70).Pct); // caps at 100
        Assert.Equal(48, new ShiftTargetRow(new DateOnly(2026, 7, 20), "Day", 17).Pct);  // 17/35 = 48%
    }

    [Fact]
    public void Day_row_uses_105_target()
    {
        var row = new DayTargetRow(new DateOnly(2026, 7, 20), 100);
        Assert.Equal(105, row.Target);
        Assert.True(row.Under);
    }
}
