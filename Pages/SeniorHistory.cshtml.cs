using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Services;

namespace TL.Pages;

public class SeniorHistoryModel : PageModel
{
    private readonly HistoryListService _history;
    private readonly AdminService _admin;
    private readonly RecordDeleteService _delete;

    public SeniorHistoryModel(HistoryListService history, AdminService admin, RecordDeleteService delete)
    {
        _history = history;
        _admin = admin;
        _delete = delete;
    }

    public bool IsAdmin => _admin.IsAdmin();
    public string? From { get; set; }
    public string? To { get; set; }
    public string? AreaFilter { get; set; }
    public string? PersonFilter { get; set; }
    public string? StatusMessage { get; set; }
    public List<SeniorHistoryRow> Rows { get; set; } = [];

    public async Task OnGetAsync(string? from, string? to, string? area, string? q, string? deleted)
    {
        From = from;
        To = to;
        AreaFilter = area;
        PersonFilter = q;
        StatusMessage = deleted == "1" ? "Record deleted." : null;
        Rows = await _history.LoadSeniorAuditsAsync(new HistoryFilters(from, to, area, q));
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        int id, string? from, string? to, string? area, string? q)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage(new { from, to, area, q });

        var ok = await _delete.DeleteAsync("senior", id, false);
        return RedirectToPage(new { from, to, area, q, deleted = ok ? "1" : null });
    }
}
