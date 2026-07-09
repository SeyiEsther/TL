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

    public record RotaWeek(int IsoWeek, DateOnly WeekStart, DateOnly WeekEnd, string Person, bool IsCurrent, bool IsPast);

    /// <summary>One row per week from the first week of the given date's year through 31 December.</summary>
    public static List<RotaWeek> WeeksThisYear(DateOnly today)
    {
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var currentMonday = today.AddDays(-daysSinceMonday);
        var yearStart = new DateOnly(today.Year, 1, 1);
        var yearStartDow = ((int)yearStart.DayOfWeek + 6) % 7;
        var firstMonday = yearStart.AddDays(-yearStartDow);
        var yearEnd = new DateOnly(today.Year, 12, 31);

        var result = new List<RotaWeek>();
        for (var cursor = firstMonday; cursor <= yearEnd; cursor = cursor.AddDays(7))
        {
            var dt = cursor.ToDateTime(TimeOnly.MinValue);
            var isoYear = ISOWeek.GetYear(dt);
            var isoWeek = ISOWeek.GetWeekOfYear(dt);
            var weekEnd = cursor.AddDays(6);
            result.Add(new RotaWeek(isoWeek, cursor, weekEnd, PersonForWeek(isoYear, isoWeek), cursor == currentMonday, weekEnd < today));
        }
        return result;
    }

    static readonly string[] AvatarPalette =
    [
        "#2B5AED", "#CC1F2C", "#16a34a", "#b45309",
        "#7c3aed", "#0891b2", "#db2777", "#65a30d",
        "#ea580c", "#4338ca", "#0d9488", "#9333ea",
    ];

    public static string AvatarColor(string person)
    {
        var idx = Array.IndexOf(Names, person);
        return AvatarPalette[(idx < 0 ? 0 : idx) % AvatarPalette.Length];
    }

    public static string Initials(string person)
    {
        var parts = person.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "?" : string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }
}
