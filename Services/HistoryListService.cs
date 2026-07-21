using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Services;

public class HistoryListService
{
    private readonly AppDbContext _db;

    public HistoryListService(AppDbContext db) => _db = db;

    public async Task<List<ShiftHistoryRow>> LoadShiftsAsync(HistoryFilters f)
    {
        var q = _db.ShiftSubmissions.ExcludeAudits();
        q = ApplyShiftFilters(q, f);

        var items = await q
            .OrderByDescending(s => s.ShiftDate)
            .Select(s => new
            {
                s.Id,
                s.ShiftDate,
                s.Shift,
                s.Area,
                s.TeamLeaderDisplay,
                s.CoveringFor,
                s.SubmittedAt,
                s.Escalations,
                Safety = s.Hours.Where(h => h.OverallSafetyStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallSafetyStatus).FirstOrDefault(),
                Quality = s.Hours.Where(h => h.OverallQualityStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallQualityStatus).FirstOrDefault(),
                Perf = s.Hours.Where(h => h.OverallPerfStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallPerfStatus).FirstOrDefault(),
            })
            .ToListAsync();

        return items.Select(s => new ShiftHistoryRow(
            s.Id, s.ShiftDate, s.Shift, s.Area ?? "—",
            PersonDisplay(s.TeamLeaderDisplay, s.CoveringFor),
            s.Safety, s.Quality, s.Perf,
            !string.IsNullOrEmpty(s.Escalations), s.SubmittedAt)).ToList();
    }

    public async Task<List<HistoryRow>> LoadHistoryShiftsAsync(HistoryFilters f, bool inProgressOnly = false)
    {
        var q = _db.ShiftSubmissions.ExcludeAudits();
        if (inProgressOnly)
            q = q.Where(s => string.IsNullOrEmpty(s.OutgoingTLSignature));
        q = ApplyShiftFilters(q, f);

        var items = await q
            .OrderByDescending(s => s.ShiftDate)
            .Select(s => new
            {
                s.Id,
                s.ShiftDate,
                s.Shift,
                s.Area,
                s.TeamLeaderDisplay,
                s.CoveringFor,
                s.SubmittedAt,
                s.Escalations,
                Safety = s.Hours.Where(h => h.OverallSafetyStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallSafetyStatus).FirstOrDefault(),
                Quality = s.Hours.Where(h => h.OverallQualityStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallQualityStatus).FirstOrDefault(),
                Perf = s.Hours.Where(h => h.OverallPerfStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallPerfStatus).FirstOrDefault(),
            })
            .ToListAsync();

        return items.Select(s => new HistoryRow(
            "Shift", "shifts", s.Id, s.ShiftDate, s.Shift, s.Area ?? "—",
            PersonDisplay(s.TeamLeaderDisplay, s.CoveringFor),
            s.Safety, s.Quality, s.Perf, null,
            !string.IsNullOrEmpty(s.Escalations), s.SubmittedAt)).ToList();
    }

    public async Task<List<HistoryRow>> LoadHistoryHodAsync(HistoryFilters f)
    {
        var rows = new List<HistoryRow>();

        var newQ = _db.HodDailyAudits.AsQueryable();
        if (!string.IsNullOrEmpty(f.From) && DateOnly.TryParse(f.From, out var from))
            newQ = newQ.Where(a => a.AuditDate >= from);
        if (!string.IsNullOrEmpty(f.To) && DateOnly.TryParse(f.To, out var to))
            newQ = newQ.Where(a => a.AuditDate <= to);
        if (!string.IsNullOrEmpty(f.Area))
            newQ = newQ.Where(a => a.EffectivenessArea == f.Area || a.Area == f.Area);
        if (!string.IsNullOrEmpty(f.Person))
            newQ = newQ.Where(a => a.AuditorName.Contains(f.Person));

        var newAudits = await newQ.OrderByDescending(a => a.AuditDate).ToListAsync();
        rows.AddRange(newAudits.Select(a => new HistoryRow(
            $"HoD — {HodAuditTypes.LabelFor(a.AuditType)}", "hod", a.Id, a.AuditDate,
            a.Department, a.ResolveEffectivenessArea(), a.AuditorName,
            null, null, null, HodAuditScoring.RatingBand(a.TotalScore, a.MaxScore),
            !string.IsNullOrEmpty(a.ActionsRaised), a.SubmittedAt,
            a.MaxScore > 0 ? a.TotalScore * 100 / a.MaxScore : null,
            IsNewHodAudit: true)));

        var legacyQ = _db.ShiftSubmissions.Where(s => s.Shift == ShiftQueryExtensions.AuditPseudoShift);
        legacyQ = ApplyShiftFilters(legacyQ, f);

        var legacy = await legacyQ
            .Include(s => s.Hours)
            .OrderByDescending(s => s.ShiftDate)
            .ToListAsync();

        rows.AddRange(legacy.Select(s =>
        {
            var h = s.Hours.OrderByDescending(x => x.HourNumber).FirstOrDefault();
            return new HistoryRow(
                "HoD / Shift Mgr (legacy)", "hod", s.Id, s.ShiftDate, null, s.Area ?? "—", s.TeamLeaderDisplay,
                h?.OverallSafetyStatus, h?.OverallQualityStatus, h?.OverallPerfStatus, null,
                !string.IsNullOrEmpty(s.Escalations), s.SubmittedAt);
        }));

        return rows;
    }

    public async Task<List<HistoryRow>> LoadHistorySeniorAsync(HistoryFilters f)
    {
        var q = _db.SeniorWeeklyAudits.AsQueryable();
        if (!string.IsNullOrEmpty(f.From) && DateOnly.TryParse(f.From, out var from))
            q = q.Where(a => a.AuditDate >= from);
        if (!string.IsNullOrEmpty(f.To) && DateOnly.TryParse(f.To, out var to))
            q = q.Where(a => a.AuditDate <= to);
        if (!string.IsNullOrEmpty(f.Area))
            q = q.Where(a => a.Area == f.Area);
        if (!string.IsNullOrEmpty(f.Person))
            q = q.Where(a => a.AuditorName.Contains(f.Person));

        var items = await q.OrderByDescending(a => a.AuditDate).ToListAsync();

        return items.Select(a => new HistoryRow(
            "Senior audit", "senior", a.Id, a.AuditDate, null, a.Area, a.AuditorName,
            null, null, null, a.OverallVerdict,
            !string.IsNullOrEmpty(a.ActionsRaised), a.SubmittedAt,
            SeniorAuditScoring.OverallScore(a))).ToList();
    }

    public async Task<List<HodHistoryRow>> LoadHodAuditsAsync(HistoryFilters f)
    {
        var rows = new List<HodHistoryRow>();

        var newQ = _db.HodDailyAudits.AsQueryable();
        if (!string.IsNullOrEmpty(f.From) && DateOnly.TryParse(f.From, out var from))
            newQ = newQ.Where(a => a.AuditDate >= from);
        if (!string.IsNullOrEmpty(f.To) && DateOnly.TryParse(f.To, out var to))
            newQ = newQ.Where(a => a.AuditDate <= to);
        if (!string.IsNullOrEmpty(f.Area))
            newQ = newQ.Where(a => a.EffectivenessArea == f.Area || a.Area == f.Area);
        if (!string.IsNullOrEmpty(f.Person))
            newQ = newQ.Where(a => a.AuditorName.Contains(f.Person));

        var newAudits = await newQ.OrderByDescending(a => a.AuditDate).ToListAsync();
        rows.AddRange(newAudits.Select(a => new HodHistoryRow(
            a.Id, a.AuditDate, HodAuditTypes.LabelFor(a.AuditType), a.Department,
            a.ResolveEffectivenessArea(), a.AuditorName,
            HodAuditScoring.RatingBand(a.TotalScore, a.MaxScore),
            a.MaxScore > 0 ? a.TotalScore * 100 / a.MaxScore : null,
            !string.IsNullOrEmpty(a.ActionsRaised), a.SubmittedAt,
            IsLegacy: false)));

        var legacyQ = _db.ShiftSubmissions.Where(s => s.Shift == ShiftQueryExtensions.AuditPseudoShift);
        legacyQ = ApplyShiftFilters(legacyQ, f);

        var legacy = await legacyQ
            .Include(s => s.Hours)
            .OrderByDescending(s => s.ShiftDate)
            .ToListAsync();

        rows.AddRange(legacy.Select(s =>
        {
            var h = s.Hours.OrderByDescending(x => x.HourNumber).FirstOrDefault();
            return new HodHistoryRow(
                s.Id, s.ShiftDate, "Legacy audit", "—", s.Area ?? "—", s.TeamLeaderDisplay,
                null, null, !string.IsNullOrEmpty(s.Escalations), s.SubmittedAt,
                IsLegacy: true,
                LegacySafety: h?.OverallSafetyStatus,
                LegacyQuality: h?.OverallQualityStatus,
                LegacyPerf: h?.OverallPerfStatus);
        }));

        return rows.OrderByDescending(r => r.Date).ThenByDescending(r => r.SubmittedAt).ToList();
    }

    public async Task<List<SeniorHistoryRow>> LoadSeniorAuditsAsync(HistoryFilters f)
    {
        var q = _db.SeniorWeeklyAudits.AsQueryable();
        if (!string.IsNullOrEmpty(f.From) && DateOnly.TryParse(f.From, out var from))
            q = q.Where(a => a.AuditDate >= from);
        if (!string.IsNullOrEmpty(f.To) && DateOnly.TryParse(f.To, out var to))
            q = q.Where(a => a.AuditDate <= to);
        if (!string.IsNullOrEmpty(f.Area))
            q = q.Where(a => a.Area == f.Area);
        if (!string.IsNullOrEmpty(f.Person))
            q = q.Where(a => a.AuditorName.Contains(f.Person));

        var items = await q.OrderByDescending(a => a.AuditDate).ToListAsync();

        return items.Select(a => new SeniorHistoryRow(
            a.Id, a.AuditDate, a.Area, a.AuditorName, a.OverallVerdict,
            SeniorAuditScoring.OverallScore(a),
            !string.IsNullOrEmpty(a.ActionsRaised), a.SubmittedAt)).ToList();
    }

    static IQueryable<ShiftSubmission> ApplyShiftFilters(IQueryable<ShiftSubmission> q, HistoryFilters f)
    {
        if (!string.IsNullOrEmpty(f.From) && DateOnly.TryParse(f.From, out var from))
            q = q.Where(s => s.ShiftDate >= from);
        if (!string.IsNullOrEmpty(f.To) && DateOnly.TryParse(f.To, out var to))
            q = q.Where(s => s.ShiftDate <= to);
        if (!string.IsNullOrEmpty(f.Area))
            q = q.Where(s => s.Area == f.Area);
        if (!string.IsNullOrEmpty(f.Person))
            q = q.Where(s => s.TeamLeaderDisplay.Contains(f.Person));
        if (!string.IsNullOrEmpty(f.Shift))
            q = q.Where(s => s.Shift == f.Shift);
        return q;
    }

    public static string PersonDisplay(string teamLeader, string? coveringFor) =>
        string.IsNullOrWhiteSpace(coveringFor) ? teamLeader : $"{teamLeader} (covering for {coveringFor})";

    public static string Rc(string? v) => v switch
    {
        "Green" or "Excellent" or "Good" => "g",
        "Amber" or "Needs Improvement" => "a",
        "Red" or "Poor" => "r",
        _ => "u",
    };
}

public record HistoryFilters(string? From, string? To, string? Area, string? Person, string? Shift = null);

public record ShiftHistoryRow(
    int Id,
    DateOnly Date,
    string Shift,
    string Area,
    string Person,
    string? Safety,
    string? Quality,
    string? Perf,
    bool HasActions,
    DateTime SubmittedAt);

public record HodHistoryRow(
    int Id,
    DateOnly Date,
    string AuditType,
    string Department,
    string Area,
    string Auditor,
    string? Rating,
    int? ScorePct,
    bool HasActions,
    DateTime SubmittedAt,
    bool IsLegacy,
    string? LegacySafety = null,
    string? LegacyQuality = null,
    string? LegacyPerf = null);

public record SeniorHistoryRow(
    int Id,
    DateOnly Date,
    string Area,
    string Auditor,
    string? Verdict,
    int Score,
    bool HasActions,
    DateTime SubmittedAt);

public record HistoryRow(
    string TypeLabel,
    string TabKey,
    int Id,
    DateOnly Date,
    string? ShiftOrExtra,
    string Area,
    string Person,
    string? Safety,
    string? Quality,
    string? Perf,
    string? Verdict,
    bool HasActions,
    DateTime SubmittedAt,
    int? Score = null,
    bool IsNewHodAudit = false);
