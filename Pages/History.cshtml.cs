using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Services;

namespace TL.Pages;

public class HistoryModel : PageModel
{
    private static readonly HashSet<string> ValidTabs = new(StringComparer.OrdinalIgnoreCase)
        { "all", "shifts", "hod", "senior" };

    private readonly HistoryListService _history;
    private readonly AdminService _admin;
    private readonly RecordDeleteService _delete;

    public HistoryModel(HistoryListService history, AdminService admin, RecordDeleteService delete)
    {
        _history = history;
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
        var filters = new HistoryFilters(From, To, AreaFilter, PersonFilter);
        var rows = new List<HistoryRow>();

        if (Tab is "shifts")
            rows.AddRange(await _history.LoadHistoryShiftsAsync(filters));
        else if (Tab is "all")
            rows.AddRange(await _history.LoadHistoryShiftsAsync(filters, inProgressOnly: true));
        if (Tab is "all" or "hod")
        {
            try { rows.AddRange(await _history.LoadHistoryHodAsync(filters)); }
            catch { /* Hod table may not exist yet */ }
        }
        if (Tab is "all" or "senior")
        {
            try { rows.AddRange(await _history.LoadHistorySeniorAsync(filters)); }
            catch { /* Senior table may not exist yet */ }
        }

        Rows = rows
            .OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.SubmittedAt)
            .ToList();
    }

    public static string Rc(string? v) => HistoryListService.Rc(v);

    public string TabLabel(string t) => t switch
    {
        "all" => "All",
        "shifts" => "Shifts",
        "hod" => "HoD / Shift Mgr",
        "senior" => "Senior",
        _ => t
    };
}
