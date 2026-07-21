using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Services;

namespace TL.Pages;

public class ShiftHistoryModel : PageModel
{
    private readonly HistoryListService _history;
    private readonly AdminService _admin;
    private readonly RecordDeleteService _delete;

    public ShiftHistoryModel(HistoryListService history, AdminService admin, RecordDeleteService delete)
    {
        _history = history;
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
    public List<ShiftHistoryRow> Rows { get; set; } = [];

    public async Task OnGetAsync(string? from, string? to, string? area, string? shift, string? q, string? deleted)
    {
        From = from;
        To = to;
        AreaFilter = area;
        ShiftFilter = shift;
        PersonFilter = q;
        StatusMessage = deleted == "1" ? "Record deleted." : null;
        Rows = await _history.LoadShiftsAsync(new HistoryFilters(from, to, area, q, shift));
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
