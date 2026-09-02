using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

// Accountability view: who was on the senior-audit rota each week and whether
// they actually completed a senior audit that week. The expectation comes from
// the existing SeniorRota (its weekly duty group); completion comes from
// SeniorWeeklyAudits. No new schema — both already exist.
public class SeniorAccountabilityModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly PersonListService _people;

    public SeniorAccountabilityModel(AppDbContext db, PersonListService people)
    {
        _db = db;
        _people = people;
    }

    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public bool MissedOnly { get; set; }

    public List<WeekCol> Weeks { get; set; } = [];
    public List<PersonRow> People { get; set; } = [];
    public int TotalExpected { get; set; }
    public int TotalCompleted { get; set; }

    public record WeekCol(int IsoWeek, DateOnly Start, DateOnly End, string Label);
    // Cell: null = not on rota that week; true = completed; false = missed.
    public record PersonRow(string Person, List<bool?> Cells, int Expected, int Completed)
    {
        public int Missed => Expected - Completed;
    }

    public async Task OnGetAsync(string? from, string? to, bool missed)
    {
        await BuildAsync(from, to, missed);
    }

    public async Task<IActionResult> OnGetCsvAsync(string? from, string? to, bool missed)
    {
        await BuildAsync(from, to, missed);
        var sb = new StringBuilder();
        sb.Append("Person");
        foreach (var w in Weeks) sb.Append(',').Append(w.Label);
        sb.Append(",Expected,Completed,Missed\n");
        foreach (var p in People)
        {
            sb.Append(Csv(p.Person));
            for (int i = 0; i < Weeks.Count; i++)
                sb.Append(',').Append(p.Cells[i] switch { true => "Completed", false => "MISSED", _ => "" });
            sb.Append(',').Append(p.Expected).Append(',').Append(p.Completed).Append(',').Append(p.Missed).Append('\n');
        }
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv",
            $"SeniorAuditAccountability_{From:yyyyMMdd}_{To:yyyyMMdd}.csv");
    }

    static string Csv(string s) => s.Contains(',') || s.Contains('"')
        ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    async Task BuildAsync(string? from, string? to, bool missed)
    {
        MissedOnly = missed;
        var today = DateOnly.FromDateTime(DateTime.Today);
        To = DateOnly.TryParse(to, out var t) ? t : today;
        From = DateOnly.TryParse(from, out var f) ? f : To.AddDays(-7 * 12); // last 12 weeks

        var names = _people.Seniors;

        // Monday-aligned weeks across the range (cap to keep the grid sane).
        var startMonday = From.AddDays(-(((int)From.DayOfWeek + 6) % 7));
        for (var cur = startMonday; cur <= To && Weeks.Count < 53; cur = cur.AddDays(7))
        {
            var dt = cur.ToDateTime(TimeOnly.MinValue);
            Weeks.Add(new WeekCol(ISOWeek.GetWeekOfYear(dt), cur, cur.AddDays(6), $"WK{ISOWeek.GetWeekOfYear(dt)}"));
        }

        // Every senior audit in the window, once.
        var windowStart = Weeks.Count > 0 ? Weeks[0].Start : From;
        var windowEnd = Weeks.Count > 0 ? Weeks[^1].End : To;
        var audits = await _db.SeniorWeeklyAudits
            .Where(a => a.AuditDate >= windowStart && a.AuditDate <= windowEnd)
            .Select(a => new { a.AuditorName, a.AuditDate })
            .ToListAsync();

        // Expected duty group per week (from the rota), and completion per person.
        var rows = new List<PersonRow>();
        foreach (var person in names)
        {
            var cells = new List<bool?>();
            int expected = 0, completed = 0;
            foreach (var w in Weeks)
            {
                var dt = w.Start.ToDateTime(TimeOnly.MinValue);
                var team = SeniorRota.TeamForWeek(ISOWeek.GetYear(dt), w.IsoWeek, names);
                var isExpected = team.Any(n => string.Equals(n, person, StringComparison.OrdinalIgnoreCase));
                if (!isExpected) { cells.Add(null); continue; }

                expected++;
                var done = audits.Any(a =>
                    a.AuditDate >= w.Start && a.AuditDate <= w.End
                    && PortalNameMatcher.Matches(person, a.AuditorName));
                if (done) completed++;
                cells.Add(done);
            }
            if (expected > 0)
                rows.Add(new PersonRow(person, cells, expected, completed));
        }

        TotalExpected = rows.Sum(r => r.Expected);
        TotalCompleted = rows.Sum(r => r.Completed);

        // Non-completions first is the primary use; sort worst offenders to the top.
        rows = rows.OrderByDescending(r => r.Missed).ThenBy(r => r.Person).ToList();
        if (MissedOnly) rows = rows.Where(r => r.Missed > 0).ToList();
        People = rows;
    }
}
