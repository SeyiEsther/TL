using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

// Read-only A3 board printout: one Head of Department's audit performance over a
// rolling four-week window (current ISO week + previous three), as four trend
// charts (one per audit type) plus their raised actions. Built entirely from
// existing HodDailyAudits — no new tables, no data entry, no saving.
public class HodAuditSummaryModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ActionService _actions;
    public HodAuditSummaryModel(AppDbContext db, ActionService actions)
    {
        _db = db;
        _actions = actions;
    }

    // Selectors (screen only).
    public List<string> HodOptions { get; set; } = [];
    public string SelectedHod { get; set; } = "";
    public static readonly string[] ShiftOptions = ["Days", "Backs", "Nights"];
    public string SelectedShift { get; set; } = "Days";

    // The rolling window, oldest → newest.
    public List<WeekCol> Weeks { get; set; } = [];
    public string WeekRangeLabel { get; set; } = "";

    // One chart per audit type (in board order): the four week points, null where
    // no audit of that type exists that week (so the line breaks, not zero).
    public List<ChartSeries> Charts { get; set; } = [];

    // Historical free-text actions from the person's own audits (kept readable
    // for past records; no longer entered on new audits — item 5).
    public List<ActionItem> Actions { get; set; } = [];

    // Item 5: the actions THIS person has assigned to OTHERS through the actions
    // system — "my results, and then the actions I've given to someone". Each
    // shows who it went to, what it relates to, and its current status.
    public List<AssignedAction> AssignedByPerson { get; set; } = [];

    // Audit types in the order they appear on the 2×2 grid.
    static readonly string[] TypeOrder =
        [HodAuditTypes.Tpm, HodAuditTypes.SixS, HodAuditTypes.Quality, HodAuditTypes.PartsIdNc];

    public async Task OnGetAsync(string? hod, string? shift)
    {
        SelectedShift = ShiftOptions.Contains(shift) ? shift! : "Days";

        // HOD list = auditors who actually have audits, so charts have data.
        HodOptions = await _db.HodDailyAudits
            .Select(a => a.AuditorName)
            .Where(n => n != null && n != "")
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        SelectedHod = !string.IsNullOrWhiteSpace(hod) && HodOptions.Contains(hod)
            ? hod!
            : HodOptions.FirstOrDefault() ?? "";

        // Rolling four-week window: recomputed every load from *today*, never a
        // fixed range. currentWeekStart back to three weeks prior.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var (currentStart, _) = WeekMath.Bounds(today);
        Weeks = Enumerable.Range(0, 4)
            .Select(i => currentStart.AddDays(-7 * (3 - i)))     // oldest first
            .Select(start =>
            {
                var (s, e) = WeekMath.Bounds(start);
                return new WeekCol(s, e, WeekMath.IsoWeekNumber(s), $"WK{WeekMath.IsoWeekNumber(s)}");
            })
            .ToList();
        WeekRangeLabel = Weeks.Count > 0 ? $"{Weeks[0].Label} – {Weeks[^1].Label}" : "";

        if (string.IsNullOrEmpty(SelectedHod))
        {
            Charts = TypeOrder.Select(t => new ChartSeries(t, HodAuditTypes.LabelFor(t),
                Weeks.Select(w => new WeekPoint(w.Label, null)).ToList())).ToList();
            return;
        }

        var windowStart = Weeks[0].Start;
        var windowEnd = Weeks[^1].End;

        var audits = await _db.HodDailyAudits
            .Where(a => a.AuditorName == SelectedHod
                        && a.AuditDate >= windowStart
                        && a.AuditDate <= windowEnd
                        && a.MaxScore > 0)
            .Select(a => new { a.AuditType, a.AuditDate, a.TotalScore, a.MaxScore, a.ActionsRaised })
            .ToListAsync();

        // Map each audit to its week-column index once.
        int WeekIndexOf(DateOnly d)
        {
            var start = WeekMath.Bounds(d).Start;
            return Weeks.FindIndex(w => w.Start == start);
        }

        Charts = TypeOrder.Select(type =>
        {
            var points = Weeks.Select((w, idx) =>
            {
                var forWeek = audits
                    .Where(a => a.AuditType == type && WeekIndexOf(a.AuditDate) == idx)
                    .Select(a => a.TotalScore * 100.0 / a.MaxScore)
                    .ToList();
                // Average multiple audits in the same week; null (line break) if none.
                double? pct = forWeek.Count > 0 ? Math.Round(forWeek.Average(), 1) : null;
                return new WeekPoint(w.Label, pct);
            }).ToList();
            return new ChartSeries(type, HodAuditTypes.LabelFor(type), points);
        }).ToList();

        Actions = audits
            .Select(a =>
            {
                var s = ActionSummaryFormatter.Summarise(a.ActionsRaised);
                return new ActionItem(
                    HodAuditTypes.LabelFor(a.AuditType),
                    Weeks[Math.Max(0, WeekIndexOf(a.AuditDate))].Label,
                    a.AuditDate,
                    s.Typed,
                    s.Summary);
            })
            // Only keep rows that carry a human-typed action or a real finding
            // summary — drop the noise.
            .Where(a => !string.IsNullOrWhiteSpace(a.Text) || !string.IsNullOrWhiteSpace(a.FindingsSummary))
            .OrderByDescending(a => a.Date)
            .ToList();

        // Structured actions this person assigned to others. Matched by the same
        // display name the board is keyed on (fuzzy, to bridge AD vs picker names).
        var raised = await _actions.RaisedByUserAsync("", SelectedHod);
        AssignedByPerson = raised
            .Where(a => PortalNameMatcher.Matches(a.RaisedByName, SelectedHod))
            .Select(a => new AssignedAction(
                a.OwnerName,
                a.SourceLabel ?? BuildRelatesTo(a),
                a.Status,
                a.DueDate,
                a.RaisedAt))
            .OrderByDescending(a => a.RaisedAt)
            .ToList();
    }

    static string BuildRelatesTo(AuditAction a)
    {
        var parts = new[] { a.AuditType, a.Area }.Where(s => !string.IsNullOrWhiteSpace(s));
        var joined = string.Join(" · ", parts);
        return string.IsNullOrWhiteSpace(joined) ? ActionSourceTypes.Label(a.SourceType) : joined;
    }

    public record WeekCol(DateOnly Start, DateOnly End, int IsoWeek, string Label);
    public record WeekPoint(string Label, double? Percent);
    public record ChartSeries(string Type, string Label, List<WeekPoint> Points);
    public record ActionItem(string TypeLabel, string WeekLabel, DateOnly Date, string Text, string? FindingsSummary);
    public record AssignedAction(string AssignedTo, string RelatesTo, string Status, DateOnly? DueDate, DateTime RaisedAt);
}
