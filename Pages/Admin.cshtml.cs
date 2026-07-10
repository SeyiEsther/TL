using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class AdminModel : PageModel
{
    private static readonly HashSet<string> ValidTabs = new(StringComparer.OrdinalIgnoreCase)
        { "sessions", "teamleaders", "hod" };

    private readonly AppDbContext _db;
    private readonly AdminService _admin;
    private readonly PersonListService _people;
    private readonly RecordDeleteService _delete;

    public AdminModel(
        AppDbContext db, AdminService admin, PersonListService people, RecordDeleteService delete)
    {
        _db = db;
        _admin = admin;
        _people = people;
        _delete = delete;
    }

    public string Tab { get; set; } = "sessions";
    public string? StatusMessage { get; set; }
    public string? Error { get; set; }
    public List<InProgressShiftRow> InProgressShifts { get; set; } = [];
    public IReadOnlyList<PickerPerson> TeamLeaderPeople { get; set; } = [];
    public IReadOnlyList<PickerPerson> HodPeople { get; set; } = [];
    public bool PeopleReadOnly { get; set; }

    public async Task<IActionResult> OnGetAsync(string? tab, string? saved, string? error)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        Tab = ValidTabs.Contains(tab ?? "") ? tab!.ToLowerInvariant() : "sessions";
        StatusMessage = saved switch
        {
            "deleted" => "Record deleted.",
            "added" => "Name added.",
            "removed" => "Name removed.",
            _ => null,
        };
        Error = error;

        await _people.EnsureLoadedAsync();
        await LoadTabDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteSessionAsync(int id)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        var ok = await _delete.DeleteShiftSubmissionAsync(id);
        return RedirectToPage(new { tab = "sessions", saved = ok ? "deleted" : null });
    }

    public async Task<IActionResult> OnPostAddPersonAsync(string listKind, string name, string? tab)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        var kind = listKind == PersonListKinds.Hod ? PersonListKinds.Hod : PersonListKinds.TeamLeader;
        var ok = await _people.AddPersonAsync(kind, name);
        var redirectTab = tab switch
        {
            "hod" => "hod",
            _ => "teamleaders",
        };
        return RedirectToPage(new
        {
            tab = redirectTab,
            saved = ok ? "added" : null,
            error = ok ? null : "duplicate",
        });
    }

    public async Task<IActionResult> OnPostRemovePersonAsync(int id, string? tab)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        var ok = await _people.RemovePersonAsync(id);
        return RedirectToPage(new
        {
            tab = tab == "hod" ? "hod" : "teamleaders",
            saved = ok ? "removed" : null,
        });
    }

    async Task LoadTabDataAsync()
    {
        if (Tab == "sessions")
        {
            InProgressShifts = await _db.ShiftSubmissions
                .ExcludeAudits()
                .Where(s => string.IsNullOrEmpty(s.OutgoingTLSignature))
                .OrderByDescending(s => s.LastEditedAt ?? s.SubmittedAt)
                .Select(s => new InProgressShiftRow(
                    s.Id,
                    s.ShiftDate,
                    s.Shift,
                    s.Area ?? "—",
                    s.TeamLeaderDisplay,
                    s.CoveringFor,
                    s.Hours.Count,
                    s.HoursCompleted,
                    s.LastEditedAt ?? s.SubmittedAt))
                .ToListAsync();
        }
        else
        {
            var (teamLeaders, hods, fromDatabase) = await _people.LoadPickerPeopleAsync();
            TeamLeaderPeople = teamLeaders;
            HodPeople = hods;
            PeopleReadOnly = !fromDatabase;
        }
    }

    public string TabLabel(string t) => t switch
    {
        "sessions" => "In-progress shifts",
        "teamleaders" => "Team leaders",
        "hod" => "HoD / Shift Mgr",
        _ => t,
    };

    public static string PersonLabel(string teamLeader, string? coveringFor) =>
        string.IsNullOrWhiteSpace(coveringFor) ? teamLeader : $"{teamLeader} (covering for {coveringFor})";
}

public record InProgressShiftRow(
    int Id,
    DateOnly Date,
    string Shift,
    string Area,
    string TeamLeader,
    string? CoveringFor,
    int HoursFilled,
    byte HoursTotal,
    DateTime LastActivity);
