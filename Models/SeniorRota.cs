using System.Globalization;

namespace TL.Models;

/// <summary>
/// Weekly duty rota for the Senior Weekly Audit, covering Group 1s and Directors.
/// The rotation order is reshuffled automatically every January (seeded by ISO year)
/// so it stays fresh year to year without any manual maintenance.
/// </summary>
public static class SeniorRota
{
    public static readonly string[] Names =
    [
        "Jim Gray", "John Fisher", "Steven Hawkins", "Vic Ward",
        "Simon Graham", "Lukasz Jaworski", "Dean Campbell", "Glen Atkinson",
        "Kyle Anderson", "Jonathan Maynard", "Mark Tapp", "Tony Bent",
    ];

    public static string[] OrderForYear(int year)
    {
        var order = (string[])Names.Clone();
        var rng = new Random(year);
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order;
    }

    public static string PersonForWeek(int isoYear, int isoWeek) =>
        OrderForYear(isoYear)[(isoWeek - 1) % Names.Length];

    public static string PersonForDate(DateOnly date)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue);
        return PersonForWeek(ISOWeek.GetYear(dt), ISOWeek.GetWeekOfYear(dt));
    }

    public record RotaWeek(int IsoWeek, DateOnly WeekStart, DateOnly WeekEnd, string Person, bool IsCurrent);

    /// <summary>One row per week from the current week through 31 December of the given date's year.</summary>
    public static List<RotaWeek> RemainingWeeksThisYear(DateOnly today)
    {
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysSinceMonday);
        var yearEnd = new DateOnly(today.Year, 12, 31);

        var result = new List<RotaWeek>();
        for (var cursor = monday; cursor <= yearEnd; cursor = cursor.AddDays(7))
        {
            var dt = cursor.ToDateTime(TimeOnly.MinValue);
            var isoYear = ISOWeek.GetYear(dt);
            var isoWeek = ISOWeek.GetWeekOfYear(dt);
            result.Add(new RotaWeek(isoWeek, cursor, cursor.AddDays(6), PersonForWeek(isoYear, isoWeek), cursor == monday));
        }
        return result;
    }
}
