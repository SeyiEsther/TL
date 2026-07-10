using System.Globalization;

namespace TL.Models;

/// <summary>
/// Weekly duty rota for the Senior Weekly Audit, covering Group 1s and Directors.
/// The 12 names are split into 3 fixed teams of 4, and one team is on duty each
/// week (cycling every 3 weeks). The teams are re-drawn automatically every January
/// (seeded by ISO year) so they stay fresh year to year without any manual upkeep.
/// </summary>
public static class SeniorRota
{
    public const int GroupSize = 4;

    public static readonly string[] Names =
    [
        "Jim Gray", "John Fisher", "Steven Hawkins", "Vic Ward",
        "Simon Graham", "Lukasz Jaworski", "Dean Campbell", "Glen Atkinson",
        "Kyle Anderson", "Jonathan Maynard", "Mark Tapp", "Tony Bent",
    ];

    public static int GroupCount => Names.Length / GroupSize;

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

    /// <summary>The fixed teams of 4 for the given year, drawn from that year's shuffle.</summary>
    public static List<string[]> TeamsForYear(int year) =>
        OrderForYear(year)
            .Select((name, i) => (name, i))
            .GroupBy(x => x.i / GroupSize)
            .Select(g => g.Select(x => x.name).ToArray())
            .ToList();

    /// <summary>Zero-based index of the team on duty for a given ISO week.</summary>
    public static int TeamIndexForWeek(int isoWeek) => (isoWeek - 1) % GroupCount;

    public static string[] TeamForWeek(int isoYear, int isoWeek) =>
        TeamsForYear(isoYear)[TeamIndexForWeek(isoWeek)];

    public record RotaWeek(int IsoWeek, DateOnly WeekStart, DateOnly WeekEnd, string[] Team, int TeamNumber, bool IsCurrent, bool IsPast);

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
            result.Add(new RotaWeek(isoWeek, cursor, weekEnd,
                TeamForWeek(isoYear, isoWeek), TeamIndexForWeek(isoWeek) + 1,
                cursor == currentMonday, weekEnd < today));
        }
        return result;
    }

    public static RotaWeek? CurrentWeek(DateOnly today) =>
        WeeksThisYear(today).FirstOrDefault(w => w.IsCurrent);

    public static RotaWeek? WeekForDate(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        var weekStart = date.AddDays(-daysSinceMonday);
        return WeeksThisYear(date).FirstOrDefault(w => w.WeekStart == weekStart);
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
