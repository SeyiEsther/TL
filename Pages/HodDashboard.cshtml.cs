using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class HodDashboardModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ShiftCompletionService _completion;

    public HodDashboardModel(AppDbContext db, ShiftCompletionService completion)
    {
        _db = db;
        _completion = completion;
    }

    public string? From { get; set; }
    public string? To { get; set; }
    public string? AreaFilter { get; set; }
    public string? DepartmentFilter { get; set; }

    public List<HodDailyAudit> Audits { get; set; } = [];
    public HodDailyAudit? Latest { get; set; }
    public int TotalAudits { get; set; }
    public int ThisWeekAudits { get; set; }
    public int TodayAudits { get; set; }
    public int AvgScorePct { get; set; }
    public int Excellent { get; set; }
    public int Good { get; set; }
    public int NeedsImprovement { get; set; }
    public int Poor { get; set; }
    public int SixSCount { get; set; }
    public int TpmCount { get; set; }
    public int PartsCount { get; set; }
    public int QualityCount { get; set; }

    public string TodaysSuggestedType { get; set; } = "";
    public string TodaysSuggestedLabel { get; set; } = "";
    public List<HodRotationRow> WeekRotation { get; set; } = [];

    public int TlShiftCount { get; set; }
    public int TlNotClosed { get; set; }
    public int TlIncomplete { get; set; }
    public int TlUnsigned { get; set; }
    public int TlHandoverPending { get; set; }
    public List<HodAreaComplianceRow> AreaCompliance { get; set; } = [];
    public List<HodAreaAuditRow> AreaAuditScores { get; set; } = [];

    public string[] ActivityLabels { get; set; } = [];
    public int[] ActivityData { get; set; } = [];
    public int[] ActivityScoreData { get; set; } = [];

    public async Task OnGetAsync(string? from, string? to, string? area, string? department)
    {
        From = from;
        To = to;
        AreaFilter = area;
        DepartmentFilter = department;

        var today = DateOnly.FromDateTime(DateTime.Today);
        TodaysSuggestedType = HodAuditTypes.SuggestedForDay(DateTime.Today.DayOfWeek);
        TodaysSuggestedLabel = HodAuditTypes.LabelFor(TodaysSuggestedType);

        var (weekStart, weekEnd) = HodEffectivenessService.WeekRange(today);

        try
        {
            var q = _db.HodDailyAudits.AsQueryable();
            if (!string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f)) q = q.Where(a => a.AuditDate >= f);
            if (!string.IsNullOrEmpty(to) && DateOnly.TryParse(to, out var t)) q = q.Where(a => a.AuditDate <= t);
            if (!string.IsNullOrEmpty(area)) q = q.Where(a => a.Area == area);
            if (!string.IsNullOrEmpty(department)) q = q.Where(a => a.Department == department);

            Audits = await q.OrderByDescending(a => a.AuditDate).ThenByDescending(a => a.SubmittedAt).ToListAsync();
        }
        catch
        {
            Audits = [];
        }

        TotalAudits = Audits.Count;
        Latest = Audits.FirstOrDefault();
        ThisWeekAudits = Audits.Count(a => a.AuditDate >= weekStart && a.AuditDate <= weekEnd);
        TodayAudits = Audits.Count(a => a.AuditDate == today);

        if (TotalAudits > 0)
        {
            AvgScorePct = (int)Audits
                .Where(a => a.MaxScore > 0)
                .Select(a => a.TotalScore * 100.0 / a.MaxScore)
                .DefaultIfEmpty(0)
                .Average();

            foreach (var audit in Audits)
            {
                var band = HodAuditScoring.RatingBand(audit.TotalScore, audit.MaxScore);
                switch (band)
                {
                    case "Excellent": Excellent++; break;
                    case "Good": Good++; break;
                    case "Needs Improvement": NeedsImprovement++; break;
                    case "Poor": Poor++; break;
                }

                switch (audit.AuditType)
                {
                    case HodAuditTypes.SixS: SixSCount++; break;
                    case HodAuditTypes.Tpm: TpmCount++; break;
                    case HodAuditTypes.PartsIdNc: PartsCount++; break;
                    case HodAuditTypes.Quality: QualityCount++; break;
                }
            }
        }

        BuildWeekRotation(weekStart, weekEnd);
        await LoadTlComplianceAsync(weekStart, weekEnd, area, department);
        BuildAreaAuditScores();
        BuildActivityCharts();
    }

    void BuildWeekRotation(DateOnly weekStart, DateOnly weekEnd)
    {
        WeekRotation = [];
        for (var d = weekStart; d <= weekEnd; d = d.AddDays(1))
        {
            var suggested = HodAuditTypes.SuggestedForDate(d);
            var done = Audits.Where(a => a.AuditDate == d).ToList();
            WeekRotation.Add(new HodRotationRow(
                d,
                d.DayOfWeek.ToString(),
                HodAuditTypes.LabelFor(suggested),
                suggested,
                done.Count,
                done.Select(a => $"{a.Area} ({HodAuditTypes.LabelFor(a.AuditType)})").ToList()));
        }
    }

    async Task LoadTlComplianceAsync(DateOnly weekStart, DateOnly weekEnd, string? area, string? department)
    {
        var deptAreas = AreaList.All
            .Where(a => a.Group == department)
            .Select(a => a.Label)
            .ToHashSet();

        var q = _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .Where(s => s.ShiftDate >= weekStart && s.ShiftDate <= weekEnd);

        if (!string.IsNullOrEmpty(area))
            q = q.Where(s => s.Area == area);
        else if (!string.IsNullOrEmpty(department))
            q = q.Where(s => s.Area != null && deptAreas.Contains(s.Area));

        var shifts = await q.ToListAsync();
        TlShiftCount = shifts.Count;

        var rows = new List<(string Area, bool Complete, bool Signed, bool HandoverPending)>();
        foreach (var shift in shifts)
        {
            var comp = _completion.Evaluate(shift);
            var signed = comp.SignedOff;
            var handoverPending = signed && string.IsNullOrWhiteSpace(shift.IncomingTLSignature);
            rows.Add((shift.Area ?? "Unknown", comp.IsComplete, signed, handoverPending));

            if (!comp.IsComplete) TlIncomplete++;
            if (!signed) TlUnsigned++;
            if (handoverPending) TlHandoverPending++;
        }

        TlNotClosed = shifts.Count(s =>
        {
            var comp = _completion.Evaluate(s);
            var signed = comp.SignedOff;
            var handover = signed && string.IsNullOrWhiteSpace(s.IncomingTLSignature);
            return !comp.IsComplete || !signed || handover;
        });

        AreaCompliance = rows
            .GroupBy(r => r.Area)
            .Select(g => new HodAreaComplianceRow(
                g.Key,
                g.Count(),
                g.Count(x => !x.Complete),
                g.Count(x => !x.Signed),
                g.Count(x => x.HandoverPending)))
            .Where(r => r.Problems > 0)
            .OrderByDescending(r => r.Problems)
            .ThenBy(r => r.Area)
            .Take(12)
            .ToList();
    }

    void BuildAreaAuditScores()
    {
        AreaAuditScores = Audits
            .GroupBy(a => a.Area)
            .Select(g =>
            {
                var scores = g.Where(a => a.MaxScore > 0).Select(a => a.TotalScore * 100 / a.MaxScore).ToList();
                var avg = scores.Count > 0 ? (int)scores.Average() : 0;
                var poor = g.Count(a => HodAuditScoring.RatingBand(a.TotalScore, a.MaxScore) is "Poor" or "Needs Improvement");
                return new HodAreaAuditRow(g.Key, g.Count(), avg, poor);
            })
            .OrderBy(r => r.AvgScorePct)
            .ThenByDescending(r => r.PoorCount)
            .Take(12)
            .ToList();
    }

    void BuildActivityCharts()
    {
        var last14 = Enumerable.Range(0, 14)
            .Select(i => DateOnly.FromDateTime(DateTime.Today.AddDays(-13 + i)))
            .ToList();

        ActivityLabels = last14.Select(d => d.ToString("dd/MM")).ToArray();
        ActivityData = last14.Select(d => Audits.Count(a => a.AuditDate == d)).ToArray();
        ActivityScoreData = last14.Select(d =>
        {
            var day = Audits.Where(a => a.AuditDate == d && a.MaxScore > 0).ToList();
            return day.Count == 0 ? 0 : (int)day.Average(a => a.TotalScore * 100.0 / a.MaxScore);
        }).ToArray();
    }

    public static int ScorePct(HodDailyAudit a) => a.MaxScore > 0 ? a.TotalScore * 100 / a.MaxScore : 0;

    public static string BandClass(HodDailyAudit a) => HodAuditScoring.RatingBand(a.TotalScore, a.MaxScore) switch
    {
        "Excellent" => "g",
        "Good" => "g",
        "Needs Improvement" => "a",
        "Poor" => "r",
        _ => "u",
    };

    public string J(object o) => System.Text.Json.JsonSerializer.Serialize(o);
}

public record HodRotationRow(
    DateOnly Date,
    string DayName,
    string SuggestedLabel,
    string SuggestedType,
    int AuditsDone,
    List<string> CompletedDetail);

public record HodAreaComplianceRow(string Area, int TotalShifts, int Incomplete, int Unsigned, int HandoverPending)
{
    public int Problems => Incomplete + Unsigned + HandoverPending;
}

public record HodAreaAuditRow(string Area, int AuditCount, int AvgScorePct, int PoorCount);
