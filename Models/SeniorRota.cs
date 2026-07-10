using System.Globalization;

namespace TL.Models;

/// <summary>
/// Weekly duty rota for the Senior Weekly Audit, covering Group 1s and Directors.
/// Names come from <see cref="PersonListService"/> in production; built-in defaults otherwise.
/// </summary>
public static class SeniorRota
{
    public const int GroupSize = 4;

    public static IReadOnlyList<string> ResolveNames(IReadOnlyList<string>? names) =>
        names is { Count: > 0 } ? names : SeniorManagementList.Names;

    public static int GroupCountFor(IReadOnlyList<string> names)
    {
        var count = ResolveNames(names).Count;
        return Math.Max(1, (count + GroupSize - 1) / GroupSize);
    }

    public static string[] OrderForYear(int year, IReadOnlyList<string>? names = null)
    {
        var source = ResolveNames(names);
        var order = source.ToArray();
        var rng = new Random(year);
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order;
    }

    /// <summary>Exactly <see cref="GroupSize"/> seniors on duty (wraps fairly when headcount is not a multiple of 4).</summary>
    public static string[] DutyGroupForWeek(int isoYear, int isoWeek, IReadOnlyList<string>? names = null)
    {
        var order = OrderForYear(isoYear, names);
        var n = order.Length;
        if (n == 0) return [];
        var size = Math.Min(GroupSize, n);
        var start = ((isoWeek - 1) * GroupSize) % n;
        return Enumerable.Range(0, size).Select(k => order[(start + k) % n]).ToArray();
    }

    /// <summary>First rotation groups for the year (each padded to 4 via wrap).</summary>
    public static List<string[]> TeamsForYear(int year, IReadOnlyList<string>? names = null)
    {
        var order = OrderForYear(year, names);
        var n = order.Length;
        if (n == 0) return [];

        var teamCount = GroupCountFor(order);
        return Enumerable.Range(0, teamCount)
            .Select(t =>
            {
                var start = (t * GroupSize) % n;
                var size = Math.Min(GroupSize, n);
                return Enumerable.Range(0, size).Select(k => order[(start + k) % n]).ToArray();
            })
            .ToList();
    }

    public static int TeamIndexForWeek(int isoWeek, IReadOnlyList<string>? names = null) =>
        (isoWeek - 1) % GroupCountFor(ResolveNames(names));

    public static string[] TeamForWeek(int isoYear, int isoWeek, IReadOnlyList<string>? names = null) =>
        DutyGroupForWeek(isoYear, isoWeek, names);

    public record RotaWeek(int IsoWeek, DateOnly WeekStart, DateOnly WeekEnd, string[] Team, int TeamNumber, bool IsCurrent, bool IsPast);

    public static List<RotaWeek> WeeksThisYear(DateOnly today, IReadOnlyList<string>? names = null)
    {
        var resolved = ResolveNames(names);
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
                TeamForWeek(isoYear, isoWeek, resolved), TeamIndexForWeek(isoWeek, resolved) + 1,
                cursor == currentMonday, weekEnd < today));
        }
        return result;
    }

    public static RotaWeek? CurrentWeek(DateOnly today, IReadOnlyList<string>? names = null) =>
        WeeksThisYear(today, names).FirstOrDefault(w => w.IsCurrent);

    public static RotaWeek? WeekForDate(DateOnly date, IReadOnlyList<string>? names = null)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        var weekStart = date.AddDays(-daysSinceMonday);
        return WeeksThisYear(date, names).FirstOrDefault(w => w.WeekStart == weekStart);
    }

    static readonly string[] AvatarPalette =
    [
        "#2B5AED", "#CC1F2C", "#16a34a", "#b45309",
        "#7c3aed", "#0891b2", "#db2777", "#65a30d",
        "#ea580c", "#4338ca", "#0d9488", "#9333ea",
    ];

    public static string AvatarColor(string person, IReadOnlyList<string>? names = null)
    {
        var idx = Array.IndexOf(ResolveNames(names).ToArray(), person);
        return AvatarPalette[(idx < 0 ? 0 : idx) % AvatarPalette.Length];
    }

    public static string Initials(string person)
    {
        var parts = person.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "?" : string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }
}
