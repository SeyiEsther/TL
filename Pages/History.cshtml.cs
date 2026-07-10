using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class HistoryModel : PageModel
{
    private static readonly HashSet<string> ValidTabs = new(StringComparer.OrdinalIgnoreCase)
        { "all", "shifts", "hod", "senior", "handovers" };

    private readonly AppDbContext _db;
    private readonly AdminService _admin;
    private readonly RecordDeleteService _delete;

    public HistoryModel(AppDbContext db, AdminService admin, RecordDeleteService delete)
    {
        _db = db;
        _admin = admin;
        _delete = delete;
    }

    public bool IsAdmin => _admin.IsAdmin();

    public string Tab { get; set; } = "all";
    public string? From { get; set; }
    public string? To { get; set; }
    public string? AreaFilter { get; set; }
    public string? PersonFilter { get; set; }
    public string? StatusMessage { get; set; }
    public List<HistoryRow> Rows { get; set; } = [];

    public async Task OnGetAsync(string? tab, string? from, string? to, string? area, string? q, string? deleted)
    {
        Tab = ValidTabs.Contains(tab ?? "") ? tab!.ToLowerInvariant() : "all";
        From = from;
        To = to;
        AreaFilter = area;
        PersonFilter = q;
        StatusMessage = deleted == "1" ? "Record deleted." : null;

        await LoadRowsAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        string kind, int id, bool isNewHodAudit,
        string? tab, string? from, string? to, string? area, string? q)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage(new { tab, from, to, area, q });

        var ok = await _delete.DeleteAsync(kind, id, isNewHodAudit);
        return RedirectToPage(new { tab, from, to, area, q, deleted = ok ? "1" : null });
    }

    async Task LoadRowsAsync()
    {
        var rows = new List<HistoryRow>();

        if (Tab is "shifts")
            rows.AddRange(await LoadShiftsAsync());
        else if (Tab is "all")
            rows.AddRange(await LoadShiftsAsync(inProgressOnly: true));
        if (Tab is "all" or "hod")
            rows.AddRange(await LoadHodAuditsSafeAsync());
        if (Tab is "all" or "senior")
            rows.AddRange(await LoadSeniorAuditsSafeAsync());
        if (Tab is "all" or "handovers")
            rows.AddRange(await LoadHandoversAsync());

        Rows = rows
            .OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.SubmittedAt)
            .ToList();
    }

    async Task<List<HistoryRow>> LoadShiftsAsync(bool inProgressOnly = false)
    {
        var q = _db.ShiftSubmissions.ExcludeAudits();
        if (inProgressOnly)
            q = q.Where(s => string.IsNullOrEmpty(s.OutgoingTLSignature));
        if (!string.IsNullOrEmpty(From) && DateOnly.TryParse(From, out var f)) q = q.Where(s => s.ShiftDate >= f);
        if (!string.IsNullOrEmpty(To) && DateOnly.TryParse(To, out var t)) q = q.Where(s => s.ShiftDate <= t);
        if (!string.IsNullOrEmpty(AreaFilter)) q = q.Where(s => s.Area == AreaFilter);
        if (!string.IsNullOrEmpty(PersonFilter)) q = q.Where(s => s.TeamLeaderDisplay.Contains(PersonFilter));

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
            "Shift", "shifts", s.Id, s.ShiftDate, s.Shift, s.Area ?? "—", PersonDisplay(s.TeamLeaderDisplay, s.CoveringFor),
            s.Safety, s.Quality, s.Perf, null,
            !string.IsNullOrEmpty(s.Escalations), false, s.SubmittedAt)).ToList();
    }

    async Task<List<HistoryRow>> LoadHodAuditsSafeAsync()
    {
        try { return await LoadHodAuditsAsync(); }
        catch { return []; }
    }

    async Task<List<HistoryRow>> LoadSeniorAuditsSafeAsync()
    {
        try { return await LoadSeniorAuditsAsync(); }
        catch { return []; }
    }

    async Task<List<HistoryRow>> LoadHodAuditsAsync()
    {
        var rows = new List<HistoryRow>();

        var newQ = _db.HodDailyAudits.AsQueryable();
        if (!string.IsNullOrEmpty(From) && DateOnly.TryParse(From, out var f)) newQ = newQ.Where(a => a.AuditDate >= f);
        if (!string.IsNullOrEmpty(To) && DateOnly.TryParse(To, out var t)) newQ = newQ.Where(a => a.AuditDate <= t);
        if (!string.IsNullOrEmpty(AreaFilter))
            newQ = newQ.Where(a => a.EffectivenessArea == AreaFilter || a.Area == AreaFilter);
        if (!string.IsNullOrEmpty(PersonFilter)) newQ = newQ.Where(a => a.AuditorName.Contains(PersonFilter));

        var newAudits = await newQ.OrderByDescending(a => a.AuditDate).ToListAsync();
        rows.AddRange(newAudits.Select(a => new HistoryRow(
            $"HoD — {HodAuditTypes.LabelFor(a.AuditType)}", "hod", a.Id, a.AuditDate,
            a.Department, a.ResolveEffectivenessArea(), a.AuditorName,
            null, null, null, HodAuditScoring.RatingBand(a.TotalScore, a.MaxScore),
            !string.IsNullOrEmpty(a.ActionsRaised), false, a.SubmittedAt,
            a.MaxScore > 0 ? a.TotalScore * 100 / a.MaxScore : null,
            IsNewHodAudit: true)));

        var q = _db.ShiftSubmissions.Where(s => s.Shift == ShiftQueryExtensions.AuditPseudoShift);
        if (!string.IsNullOrEmpty(From) && DateOnly.TryParse(From, out var f2)) q = q.Where(s => s.ShiftDate >= f2);
        if (!string.IsNullOrEmpty(To) && DateOnly.TryParse(To, out var t2)) q = q.Where(s => s.ShiftDate <= t2);
        if (!string.IsNullOrEmpty(AreaFilter)) q = q.Where(s => s.Area == AreaFilter);
        if (!string.IsNullOrEmpty(PersonFilter)) q = q.Where(s => s.TeamLeaderDisplay.Contains(PersonFilter));

        var items = await q
            .Include(s => s.Hours)
            .OrderByDescending(s => s.ShiftDate)
            .ToListAsync();

        rows.AddRange(items.Select(s =>
        {
            var h = s.Hours.OrderByDescending(x => x.HourNumber).FirstOrDefault();
            return new HistoryRow(
                "HoD / Shift Mgr (legacy)", "hod", s.Id, s.ShiftDate, null, s.Area ?? "—", s.TeamLeaderDisplay,
                h?.OverallSafetyStatus, h?.OverallQualityStatus, h?.OverallPerfStatus, null,
                !string.IsNullOrEmpty(s.Escalations), false, s.SubmittedAt);
        }));

        return rows;
    }

    async Task<List<HistoryRow>> LoadSeniorAuditsAsync()
    {
        var q = _db.SeniorWeeklyAudits.AsQueryable();
        if (!string.IsNullOrEmpty(From) && DateOnly.TryParse(From, out var f)) q = q.Where(a => a.AuditDate >= f);
        if (!string.IsNullOrEmpty(To) && DateOnly.TryParse(To, out var t)) q = q.Where(a => a.AuditDate <= t);
        if (!string.IsNullOrEmpty(AreaFilter)) q = q.Where(a => a.Area == AreaFilter);
        if (!string.IsNullOrEmpty(PersonFilter)) q = q.Where(a => a.AuditorName.Contains(PersonFilter));

        var items = await q.OrderByDescending(a => a.AuditDate).ToListAsync();

        return items.Select(a => new HistoryRow(
            "Senior audit", "senior", a.Id, a.AuditDate, null, a.Area, a.AuditorName,
            null, null, null, a.OverallVerdict,
            !string.IsNullOrEmpty(a.ActionsRaised), false, a.SubmittedAt,
            SeniorAuditScoring.OverallScore(a))).ToList();
    }

    async Task<List<HistoryRow>> LoadHandoversAsync()
    {
        var q = _db.ShiftSubmissions
            .ExcludeAudits()
            .Where(s => !string.IsNullOrEmpty(s.OutgoingTLSignature));
        if (!string.IsNullOrEmpty(From) && DateOnly.TryParse(From, out var f)) q = q.Where(s => s.ShiftDate >= f);
        if (!string.IsNullOrEmpty(To) && DateOnly.TryParse(To, out var t)) q = q.Where(s => s.ShiftDate <= t);
        if (!string.IsNullOrEmpty(AreaFilter)) q = q.Where(s => s.Area == AreaFilter);
        if (!string.IsNullOrEmpty(PersonFilter)) q = q.Where(s => s.TeamLeaderDisplay.Contains(PersonFilter));

        var items = await q
            .Include(s => s.Hours)
            .OrderByDescending(s => s.ShiftDate)
            .ToListAsync();

        return items.Select(s =>
        {
            var h = s.Hours.OrderByDescending(x => x.HourNumber).FirstOrDefault();
            var pending = string.IsNullOrEmpty(s.IncomingTLSignature);
            return new HistoryRow(
                pending ? "Handover (pending)" : "Handover", "handovers", s.Id, s.ShiftDate, s.Shift,
                s.Area ?? "—", PersonDisplay(s.TeamLeaderDisplay, s.CoveringFor),
                h?.OverallSafetyStatus, h?.OverallQualityStatus, h?.OverallPerfStatus, null,
                false, pending, s.SubmittedAt);
        }).ToList();
    }

    static string PersonDisplay(string teamLeader, string? coveringFor) =>
        string.IsNullOrWhiteSpace(coveringFor) ? teamLeader : $"{teamLeader} (covering for {coveringFor})";

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };

    public string TabLabel(string t) => t switch
    {
        "all" => "All",
        "shifts" => "Shifts",
        "hod" => "HoD / Shift Mgr",
        "senior" => "Senior",
        "handovers" => "Handovers",
        _ => t
    };
}

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
    bool HandoverPending,
    DateTime SubmittedAt,
    int? Score = null,
    bool IsNewHodAudit = false);
