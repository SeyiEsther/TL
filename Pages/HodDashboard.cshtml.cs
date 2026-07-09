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
    public List<HodTlAccountabilityRow> TlAccountability { get; set; } = [];
    public List<HodTlShiftIssueRow> TlShiftIssues { get; set; } = [];
    public List<HodTlAuditCatchRow> TlAuditCatches { get; set; } = [];
    public int TlWithIssues { get; set; }
    public string CompliancePeriodLabel { get; set; } = "";

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
            if (!string.IsNullOrEmpty(area))
                q = q.Where(a => a.EffectivenessArea == area || a.Area == area);
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

        var complianceStart = !string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var cf) ? cf : weekStart;
        var complianceEnd = !string.IsNullOrEmpty(to) && DateOnly.TryParse(to, out var ct) ? ct : weekEnd;
        CompliancePeriodLabel = complianceStart == complianceEnd
            ? complianceStart.ToString("dd/MM/yyyy")
            : $"{complianceStart:dd/MM/yyyy} – {complianceEnd:dd/MM/yyyy}";

        await LoadTlComplianceAsync(complianceStart, complianceEnd, area, department);
        BuildTlAuditCatches();
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
                done.Select(a => $"{a.Department} / {a.ResolveEffectivenessArea()} ({HodAuditTypes.LabelFor(a.AuditType)})").ToList()));
        }
    }

    async Task LoadTlComplianceAsync(DateOnly rangeStart, DateOnly rangeEnd, string? area, string? department)
    {
        var deptAreas = AreaList.All
            .Where(a => a.Group == department)
            .Select(a => a.Label)
            .ToHashSet();

        var q = _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .Where(s => s.ShiftDate >= rangeStart && s.ShiftDate <= rangeEnd);

        if (!string.IsNullOrEmpty(area))
            q = q.Where(s => s.Area == area);
        else if (!string.IsNullOrEmpty(department))
            q = q.Where(s => s.Area != null && deptAreas.Contains(s.Area));

        var shifts = await q.OrderByDescending(s => s.ShiftDate).ThenBy(s => s.Shift).ToListAsync();
        TlShiftCount = shifts.Count;

        var issueRows = new List<HodTlShiftIssueRow>();
        var tlStats = new Dictionary<string, (int Total, int Problems, int Incomplete, int Unsigned, int Handover, HashSet<string> Areas, List<string> Summaries)>(StringComparer.OrdinalIgnoreCase);

        foreach (var shift in shifts)
        {
            var comp = _completion.Evaluate(shift);
            var signed = comp.SignedOff;
            var handoverPending = signed && string.IsNullOrWhiteSpace(shift.IncomingTLSignature);
            var closeStatus = ResolveCloseStatus(comp, signed, handoverPending);
            var hasProblem = !comp.IsComplete || !signed || handoverPending;

            if (!comp.IsComplete) TlIncomplete++;
            if (!signed) TlUnsigned++;
            if (handoverPending) TlHandoverPending++;

            var tl = string.IsNullOrWhiteSpace(shift.TeamLeaderDisplay) ? "Unknown TL" : shift.TeamLeaderDisplay.Trim();
            if (!tlStats.TryGetValue(tl, out var stat))
                stat = (0, 0, 0, 0, 0, new HashSet<string>(), new List<string>());
            stat.Total++;
            stat.Areas.Add(shift.Area ?? "Unknown");
            if (hasProblem)
            {
                stat.Problems++;
                if (!comp.IsComplete) stat.Incomplete++;
                if (!signed) stat.Unsigned++;
                if (handoverPending) stat.Handover++;

                var issues = BuildShiftIssues(comp, signed, handoverPending);
                issueRows.Add(new HodTlShiftIssueRow(
                    shift.Id,
                    tl,
                    shift.ShiftDate,
                    shift.Shift,
                    shift.Area ?? "—",
                    closeStatus,
                    comp.HoursComplete,
                    comp.HoursTotal,
                    issues));

                var headline = $"{shift.ShiftDate:dd/MM} {shift.Shift}: {closeStatus}";
                if (!stat.Summaries.Contains(headline))
                    stat.Summaries.Add(headline);
            }
            tlStats[tl] = stat;
        }

        TlNotClosed = issueRows.Count;
        TlShiftIssues = issueRows;
        TlWithIssues = tlStats.Count(kv => kv.Value.Problems > 0);

        TlAccountability = tlStats
            .Where(kv => kv.Value.Problems > 0)
            .Select(kv => new HodTlAccountabilityRow(
                kv.Key,
                kv.Value.Areas.Count == 1 ? kv.Value.Areas.First() : $"{kv.Value.Areas.Count} areas",
                kv.Value.Total,
                kv.Value.Problems,
                kv.Value.Incomplete,
                kv.Value.Unsigned,
                kv.Value.Handover,
                kv.Value.Summaries.Take(5).ToList()))
            .OrderByDescending(r => r.ProblemShifts)
            .ThenByDescending(r => r.Incomplete)
            .ThenBy(r => r.TeamLeader)
            .ToList();

        AreaCompliance = shifts
            .GroupBy(s => s.Area ?? "Unknown")
            .Select(g =>
            {
                var areaIssues = issueRows.Where(r => r.Area == (g.Key == "Unknown" ? "—" : g.Key) || r.Area == g.Key).ToList();
                if (areaIssues.Count == 0) return null;
                return new HodAreaComplianceRow(
                    g.Key,
                    g.Count(),
                    areaIssues.Count(x => x.CloseStatus == "Incomplete form"),
                    areaIssues.Count(x => x.CloseStatus == "Not signed off"),
                    areaIssues.Count(x => x.CloseStatus == "Handover pending"));
            })
            .Where(r => r != null)
            .Cast<HodAreaComplianceRow>()
            .OrderByDescending(r => r.Problems)
            .ThenBy(r => r.Area)
            .Take(12)
            .ToList();
    }

    static string ResolveCloseStatus(ShiftCompletionResult comp, bool signed, bool handoverPending)
    {
        if (comp.IsComplete && signed && !handoverPending) return "Closed correctly";
        if (!signed) return "Not signed off";
        if (handoverPending) return "Handover pending";
        return "Incomplete form";
    }

    static List<string> BuildShiftIssues(ShiftCompletionResult comp, bool signed, bool handoverPending)
    {
        var issues = new List<string>();
        if (!comp.IsComplete)
        {
            issues.AddRange(comp.MissingItems.Take(4));
            if (comp.MissingItems.Count > 4)
                issues.Add($"+{comp.MissingItems.Count - 4} more");
        }
        if (!signed)
            issues.Add("Outgoing TL sign-off missing");
        if (handoverPending)
            issues.Add("Incoming TL has not acknowledged handover");
        return issues;
    }

    void BuildTlAuditCatches()
    {
        var catches = new List<HodTlAuditCatchRow>();
        foreach (var audit in Audits)
        {
            var findings = HodAuditSerializer.ParseEffectiveness(audit.EffectivenessJson);
            foreach (var f in findings.Where(f => !string.IsNullOrEmpty(f.TeamLeader) && (f.TlClaimMismatch || f.IsAuditFinding)))
            {
                var issues = new List<string>();
                if (f.TlClaimMismatch && f.LinkedAuditFailures.Count > 0)
                    issues.AddRange(f.LinkedAuditFailures.Select(x => $"Audit caught: {x}"));
                else if (f.TlClaimMismatch)
                    issues.Add("HoD audit failed — contradicts what TL claimed on shift form");
                if (!f.FormComplete)
                    issues.Add("Shift form incomplete when audited");
                if (!f.OutgoingSignedOff)
                    issues.Add("Not signed off");
                if (f.OutgoingSignedOff && !f.IncomingHandoverAcknowledged)
                    issues.Add("Handover not acknowledged");
                issues.AddRange(f.Issues.Where(i => i.StartsWith("Audit FAIL", StringComparison.OrdinalIgnoreCase)));

                if (issues.Count == 0) continue;

                catches.Add(new HodTlAuditCatchRow(
                    f.TeamLeader,
                    f.ShiftDate,
                    f.Shift,
                    f.Area,
                    audit.AuditDate,
                    HodAuditTypes.LabelFor(audit.AuditType),
                    issues.Distinct().ToList()));
            }
        }

        TlAuditCatches = catches
            .OrderByDescending(c => c.AuditDate)
            .ThenBy(c => c.TeamLeader)
            .ToList();
    }

    void BuildAreaAuditScores()
    {
        AreaAuditScores = Audits
            .GroupBy(a => a.Department)
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

public record HodTlAccountabilityRow(
    string TeamLeader,
    string Area,
    int TotalShifts,
    int ProblemShifts,
    int Incomplete,
    int Unsigned,
    int HandoverPending,
    List<string> IssueSummary);

public record HodTlShiftIssueRow(
    int ShiftId,
    string TeamLeader,
    DateOnly ShiftDate,
    string Shift,
    string Area,
    string CloseStatus,
    int HoursComplete,
    int HoursTotal,
    List<string> Issues);

public record HodTlAuditCatchRow(
    string TeamLeader,
    DateOnly ShiftDate,
    string Shift,
    string Area,
    DateOnly AuditDate,
    string AuditType,
    List<string> Issues);
