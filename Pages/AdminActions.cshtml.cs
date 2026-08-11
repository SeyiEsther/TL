using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Models;
using TL.Services;

namespace TL.Pages;

// Admin-only full view of every action, live and historical, with reassign and
// reopen. Uses the existing config-driven admin check.
public class AdminActionsModel : PageModel
{
    private readonly ActionService _actions;
    private readonly AdminService _admin;
    private readonly PersonListService _people;

    public AdminActionsModel(ActionService actions, AdminService admin, PersonListService people)
    {
        _actions = actions;
        _admin = admin;
        _people = people;
    }

    public string? OwnerFilter { get; set; }
    public string? AreaFilter { get; set; }
    public string? TypeFilter { get; set; }

    public List<AuditAction> Open { get; set; } = [];
    public List<AuditAction> Completed { get; set; } = [];
    public List<string> Owners { get; set; } = [];
    public List<string> Areas { get; set; } = [];
    public List<string> Types { get; set; } = [];
    public IReadOnlyList<string> OwnerChoices => _people.ActionOwnersList;

    public async Task<IActionResult> OnGetAsync(string? owner, string? area, string? type)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        OwnerFilter = owner; AreaFilter = area; TypeFilter = type;
        var all = await _actions.AllAsync();

        Owners = all.Select(a => a.OwnerName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().OrderBy(n => n).ToList();
        Areas = all.Select(a => a.Area ?? "").Where(n => n != "").Distinct().OrderBy(n => n).ToList();
        Types = all.Select(a => a.AuditType ?? "").Where(n => n != "").Distinct().OrderBy(n => n).ToList();

        IEnumerable<AuditAction> q = all;
        if (!string.IsNullOrWhiteSpace(owner)) q = q.Where(a => a.OwnerName == owner);
        if (!string.IsNullOrWhiteSpace(area)) q = q.Where(a => a.Area == area);
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(a => a.AuditType == type);
        var list = q.ToList();

        Open = list.Where(a => a.Status == ActionStatus.Open)
            .OrderByDescending(a => a.RaisedAt).ToList();
        Completed = list.Where(a => a.Status == ActionStatus.Complete)
            .OrderByDescending(a => a.CompletedAt).ToList();
        return Page();
    }

    public static string OpenDuration(AuditAction a)
    {
        if (a.CompletedAt == null) return "—";
        var days = (a.CompletedAt.Value.Date - a.RaisedAt.Date).Days;
        return days <= 0 ? "same day" : $"{days} day{(days == 1 ? "" : "s")}";
    }
}
