using System.Globalization;

namespace TL.Services;

/// <summary>
/// ISO-week helpers (Monday start). Used by the operations dashboard's weekly
/// report and the Senior weekly audit so week boundaries are identical app-wide.
/// </summary>
public static class WeekMath
{
    public static (DateOnly Start, DateOnly End) Bounds(DateOnly d)
    {
        var mondayOffset = ((int)d.DayOfWeek + 6) % 7; // Mon=0 … Sun=6
        var start = d.AddDays(-mondayOffset);
        return (start, start.AddDays(6));
    }

    public static int IsoWeekNumber(DateOnly d) =>
        ISOWeek.GetWeekOfYear(d.ToDateTime(TimeOnly.MinValue));

    public static int IsoYear(DateOnly d) =>
        ISOWeek.GetYear(d.ToDateTime(TimeOnly.MinValue));

    /// <summary>e.g. "Week 30 · 21–27 Jul 2026".</summary>
    public static string Label(DateOnly weekStart)
    {
        var (start, end) = Bounds(weekStart);
        return $"Week {IsoWeekNumber(start)} · {start:d MMM}–{end:d MMM yyyy}";
    }
}
