using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public abstract class AdminPeoplePageModel : PageModel
{
    private readonly AdminService _admin;
    private readonly PersonListService _people;
    private readonly UserService _users;

    protected AdminPeoplePageModel(AdminService admin, PersonListService people, UserService users)
    {
        _admin = admin;
        _people = people;
        _users = users;
    }

    protected abstract string ListKind { get; }
    protected abstract string PageTitle { get; }
    protected abstract string PageSubtitle { get; }
    protected abstract string HelpText { get; }

    public string Title => PageTitle;
    public string Subtitle => PageSubtitle;
    public string Help => HelpText;
    public string? StatusMessage { get; set; }
    public bool StatusIsWarning { get; set; }
    public string? Error { get; set; }
    public IReadOnlyList<PickerPerson> People { get; set; } = [];
    public bool PeopleReadOnly { get; set; }

    public async Task<IActionResult> OnGetAsync(string? saved, string? error, string? name, string? resolved)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        StatusMessage = saved switch
        {
            "added" => resolved switch
            {
                "no" => $"“{name}” was added, but no Active Directory user matches that name. " +
                        "Check the spelling against how AD shows them (e.g. formal first name) — " +
                        "otherwise they may sign in and still get no access.",
                "yes" => $"“{name}” added and matched to an Active Directory user. ✓",
                _ => "Name added.",
            },
            "removed" => "Name removed.",
            _ => null,
        };
        StatusIsWarning = saved == "added" && resolved == "no";
        Error = error;

        await _people.EnsureLoadedAsync();
        var (people, fromDatabase) = await _people.LoadPeopleByKindAsync(ListKind);
        People = people;
        PeopleReadOnly = !fromDatabase;
        return Page();
    }

    public async Task<IActionResult> OnPostAddPersonAsync(string name)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        var ok = await _people.AddPersonAsync(ListKind, name);
        // Tell the admin at the point of adding whether the name resolves to a
        // real AD user, so a typo/nickname mismatch is caught now, not later.
        string? resolved = null;
        if (ok)
        {
            var r = _users.DisplayNameResolvesToAd(name);
            resolved = r == true ? "yes" : r == false ? "no" : "unknown";
        }
        return RedirectToPage(new
        {
            saved = ok ? "added" : null,
            error = ok ? null : "duplicate",
            name = ok ? name?.Trim() : null,
            resolved,
        });
    }

    public async Task<IActionResult> OnPostRemovePersonAsync(int id)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        var ok = await _people.RemovePersonAsync(id);
        return RedirectToPage(new { saved = ok ? "removed" : null });
    }
}

public class AdminTeamLeadersModel : AdminPeoplePageModel
{
    public AdminTeamLeadersModel(AdminService admin, PersonListService people, UserService users) : base(admin, people, users) { }
    protected override string ListKind => PersonListKinds.TeamLeader;
    protected override string PageTitle => "Team leader names";
    protected override string PageSubtitle => "Names on the Home team leader picker";
    protected override string HelpText => "Changes take effect immediately — no redeploy needed.";
}

public class AdminHodNamesModel : AdminPeoplePageModel
{
    public AdminHodNamesModel(AdminService admin, PersonListService people, UserService users) : base(admin, people, users) { }
    protected override string ListKind => PersonListKinds.Hod;
    protected override string PageTitle => "HoD / Shift Manager names";
    protected override string PageSubtitle => "Names on the HoD audit start screen";
    protected override string HelpText => "Changes take effect immediately — no redeploy needed.";
}

public class AdminSeniorNamesModel : AdminPeoplePageModel
{
    public AdminSeniorNamesModel(AdminService admin, PersonListService people, UserService users) : base(admin, people, users) { }
    protected override string ListKind => PersonListKinds.Senior;
    protected override string PageTitle => "Senior management names";
    protected override string PageSubtitle => "Names for senior weekly audits and the duty rota";
    protected override string HelpText => "Changes take effect immediately — no redeploy needed.";
}

public class AdminFullAccessModel : AdminPeoplePageModel
{
    public AdminFullAccessModel(AdminService admin, PersonListService people, UserService users) : base(admin, people, users) { }
    protected override string ListKind => PersonListKinds.FullAccess;
    protected override string PageTitle => "Full access users";
    protected override string PageSubtitle => "Shift managers, Group 1, and Directors — can view everything except Admin";
    protected override string HelpText => "Changes take effect immediately — no redeploy needed.";
}

public class AdminShiftManagersModel : AdminPeoplePageModel
{
    public AdminShiftManagersModel(AdminService admin, PersonListService people, UserService users) : base(admin, people, users) { }
    protected override string ListKind => PersonListKinds.ShiftManager;
    protected override string PageTitle => "Shift manager names";
    protected override string PageSubtitle => "Names on the Shift Manager tab and daily report";
    protected override string HelpText => "Shift managers keep full access; this list drives their name pickers. Changes take effect immediately.";
}
