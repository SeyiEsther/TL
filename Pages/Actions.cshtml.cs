using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Models;
using TL.Services;

namespace TL.Pages;

// Management "Actions" tab: the safety net. Two clearly-separated groups —
// shared destinations (legitimate, e.g. Maintenance) and unresolved owners
// (name mismatches needing attention) — plus a filterable view of everything.
public class ActionsModel : PageModel
{
    private readonly ActionService _actions;
    private readonly PersonListService _people;

    public ActionsModel(ActionService actions, PersonListService people)
    {
        _actions = actions;
        _people = people;
    }

    public string StatusFilter { get; set; } = "Open";
    public string? OwnerFilter { get; set; }
    public string? AreaFilter { get; set; }

    public List<AuditAction> Unresolved { get; set; } = [];
    public List<IGrouping<string, AuditAction>> Shared { get; set; } = [];
    public List<AuditAction> Filtered { get; set; } = [];

    public List<string> Owners { get; set; } = [];
    public List<string> Areas { get; set; } = [];
    public IReadOnlyList<string> OwnerChoices => _people.ActionOwnersList;

    public async Task OnGetAsync(string? status, string? owner, string? area)
    {
        StatusFilter = status ?? "Open";
        OwnerFilter = owner;
        AreaFilter = area;

        var all = await _actions.AllAsync();
        var known = await _actions.KnownNamesAsync();

        // Unresolved: open, non-external, owner matches no known person.
        Unresolved = all
            .Where(a => a.Status == ActionStatus.Open && ActionService.IsUnresolved(a, known))
            .ToList();

        // Shared destinations: open external actions, grouped by destination.
        Shared = all
            .Where(a => a.Status == ActionStatus.Open && a.OwnerIsExternal)
            .GroupBy(a => a.OwnerName)
            .OrderBy(g => g.Key)
            .ToList();

        Owners = all.Select(a => a.OwnerName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().OrderBy(n => n).ToList();
        Areas = all.Select(a => a.Area ?? "").Where(n => n != "").Distinct().OrderBy(n => n).ToList();

        IEnumerable<AuditAction> q = all;
        if (StatusFilter is "Open" or "Complete")
            q = q.Where(a => a.Status == StatusFilter);
        if (!string.IsNullOrWhiteSpace(OwnerFilter))
            q = q.Where(a => a.OwnerName == OwnerFilter);
        if (!string.IsNullOrWhiteSpace(AreaFilter))
            q = q.Where(a => a.Area == AreaFilter);
        Filtered = q.ToList();
    }
}
