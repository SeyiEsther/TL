using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
    public string FollowUpPeriodLabel { get; set; } = "";
    public List<ShiftHistoryRow> Rows { get; set; } = [];
    public TlShiftComplianceSnapshot? FollowUp { get; set; }

    public async Task OnGetAsync(string? from, string? to, string? area, string? shift, string? q, string? deleted)
    {
        AreaFilter = area;
        ShiftFilter = shift;
        PersonFilter = q;
        StatusMessage = deleted == "1" ? "Record deleted." : null;

        // Default both the follow-up cards and the records table to the current ISO week
        // so the page reads as one scoped view (users can still widen via From/To).
        var today = DateOnly.FromDateTime(DateTime.Today);
        var (weekStart, weekEnd) = HodEffectivenessService.WeekRange(today);
        if (string.IsNullOrEmpty(from) && string.IsNullOrEmpty(to))
        {
            from = weekStart.ToString("yyyy-MM-dd");
            to = weekEnd.ToString("yyyy-MM-dd");
        }
        From = from;
        To = to;

        Rows = await _history.LoadShiftsAsync(new HistoryFilters(from, to, area, q, shift));
        FollowUp = await LoadFollowUpAsync(from, to, area, shift, q);
    }

    async Task<TlShiftComplianceSnapshot?> LoadFollowUpAsync(
        string? from, string? to, string? area, string? shift, string? q)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var (weekStart, weekEnd) = HodEffectivenessService.WeekRange(today);
        var rangeStart = !string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f) ? f : weekStart;
        var rangeEnd = !string.IsNullOrEmpty(to) && DateOnly.TryParse(to, out var t) ? t : weekEnd;

        FollowUpPeriodLabel = rangeStart == rangeEnd
            ? rangeStart.ToString("dd/MM/yyyy")
            : $"{rangeStart:dd/MM/yyyy} – {rangeEnd:dd/MM/yyyy}";

        var snapshot = await _compliance.LoadAsync(rangeStart, rangeEnd, area, shift: shift, teamLeader: q);
        return snapshot;
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
