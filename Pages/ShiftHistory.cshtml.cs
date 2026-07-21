using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class ShiftHistoryModel : PageModel
{
    private readonly HistoryListService _history;
    private readonly TlShiftComplianceService _compliance;
    private readonly AdminService _admin;
    private readonly RecordDeleteService _delete;

    public ShiftHistoryModel(
        HistoryListService history,
        TlShiftComplianceService compliance,
        AdminService admin,
        RecordDeleteService delete)
    {
        _history = history;
        _compliance = compliance;
        _admin = admin;
        _delete = delete;
    }

    public bool IsAdmin => _admin.IsAdmin();
    public string? From { get; set; }
    public string? To { get; set; }
    public string? AreaFilter { get; set; }
    public string? ShiftFilter { get; set; }
    public string? PersonFilter { get; set; }
    public string? StatusMessage { get; set; }
    public string ProblemShiftPeriodLabel { get; set; } = "";
    public List<ShiftHistoryRow> Rows { get; set; } = [];
    public List<HodTlShiftIssueRow> ProblemShifts { get; set; } = [];

    public async Task OnGetAsync(string? from, string? to, string? area, string? shift, string? q, string? deleted)
    {
        From = from;
        To = to;
        AreaFilter = area;
        ShiftFilter = shift;
        PersonFilter = q;
        StatusMessage = deleted == "1" ? "Record deleted." : null;
        Rows = await _history.LoadShiftsAsync(new HistoryFilters(from, to, area, q, shift));
        ProblemShifts = await LoadProblemShiftsAsync(from, to, area, shift, q);
    }

    async Task<List<HodTlShiftIssueRow>> LoadProblemShiftsAsync(
        string? from, string? to, string? area, string? shift, string? q)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var (weekStart, weekEnd) = HodEffectivenessService.WeekRange(today);
        var rangeStart = !string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f) ? f : weekStart;
        var rangeEnd = !string.IsNullOrEmpty(to) && DateOnly.TryParse(to, out var t) ? t : weekEnd;

        ProblemShiftPeriodLabel = rangeStart == rangeEnd
            ? rangeStart.ToString("dd/MM/yyyy")
            : $"{rangeStart:dd/MM/yyyy} – {rangeEnd:dd/MM/yyyy}";

        var snapshot = await _compliance.LoadAsync(rangeStart, rangeEnd, area);
        var issues = snapshot.ShiftIssues.AsEnumerable();

        if (!string.IsNullOrEmpty(shift))
            issues = issues.Where(s => string.Equals(s.Shift, shift, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(q))
            issues = issues.Where(s => s.TeamLeader.Contains(q, StringComparison.OrdinalIgnoreCase));

        return issues.ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        int id, string? from, string? to, string? area, string? shift, string? q)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage(new { from, to, area, shift, q });

        var ok = await _delete.DeleteShiftSubmissionAsync(id);
        return RedirectToPage(new { from, to, area, shift, q, deleted = ok ? "1" : null });
    }
}
