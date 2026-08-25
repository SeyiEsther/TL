using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class MeetingSessionModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserService _users;
    private readonly PortalAccessService _access;

    public MeetingSessionModel(AppDbContext db, UserService users, PortalAccessService access)
    {
        _db = db;
        _users = users;
        _access = access;
    }

    public bool CanEdit { get; set; }
    public int? EditingId { get; set; }
    public bool SlotMissing { get; set; }

    // Natural key (date + area + shift) — read-only context for the record.
    public string MeetingDate { get; set; } = "";
    public string Area { get; set; } = "";
    public string Shift { get; set; } = "";
    public string ShiftTime { get; set; } = "";

    [BindProperty] public string? Supervisor { get; set; }
    [BindProperty] public string? CostCentre { get; set; }
    [BindProperty] public string? Team { get; set; }
    [BindProperty] public string? CompiledOn { get; set; }
    [BindProperty] public string? Location { get; set; }
    [BindProperty] public string? MeetingDateTime { get; set; }

    [BindProperty] public string? Actions { get; set; }
    [BindProperty] public string? HealthSafety { get; set; }
    [BindProperty] public string? CustomerSatisfaction { get; set; }
    [BindProperty] public string? Kpis { get; set; }
    [BindProperty] public string? EmployeeSatisfaction { get; set; }
    [BindProperty] public string? Aob { get; set; }

    [BindProperty] public string? MeetingResults { get; set; }
    [BindProperty] public string? MinutesKeeper { get; set; }

    // Repeatable sections arrive as JSON strings built client-side.
    [BindProperty] public string? ProblemsJson { get; set; }
    [BindProperty] public string? TeamMembersJson { get; set; }
    [BindProperty] public string? GroupMembersJson { get; set; }
    [BindProperty] public string? GuestsJson { get; set; }

    public List<TeamMeetingProblem> Problems { get; set; } = [];
    public List<TeamMeetingAttendee> TeamMembers { get; set; } = [];
    public List<TeamMeetingAttendee> GroupMembers { get; set; } = [];
    public List<TeamMeetingGuest> Guests { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string? date, string? area, string? shift, int? id)
    {
        // Any signed-in user may record a team meeting.
        CanEdit = true;

        TeamMeeting? m = null;
        if (id.HasValue)
            m = await _db.TeamMeetings.FirstOrDefaultAsync(x => x.Id == id.Value);
        else if (DateOnly.TryParse(date, out var d) && !string.IsNullOrWhiteSpace(area) && MeetingShifts.IsValid(shift))
            m = await FindByKeyAsync(d, area!, shift!);

        if (m != null)
        {
            EditingId = m.Id;
            MeetingDate = m.MeetingDate.ToString("yyyy-MM-dd");
            Area = m.Area;
            Shift = m.Shift;
            ShiftTime = MeetingShifts.TimeFor(m.Shift);
            Supervisor = m.Supervisor; CostCentre = m.CostCentre; Team = m.Team;
            CompiledOn = m.CompiledOn?.ToString("yyyy-MM-dd");
            Location = m.Location; MeetingDateTime = m.MeetingDateTime;
            Actions = m.Actions; HealthSafety = m.HealthSafety;
            CustomerSatisfaction = m.CustomerSatisfaction; Kpis = m.Kpis;
            EmployeeSatisfaction = m.EmployeeSatisfaction; Aob = m.Aob;
            MeetingResults = m.MeetingResults; MinutesKeeper = m.MinutesKeeper;
            Problems = TeamMeetingSerializer.Problems(m.ProblemsJson);
            TeamMembers = TeamMeetingSerializer.Attendees(m.TeamMembersJson);
            GroupMembers = TeamMeetingSerializer.Attendees(m.GroupMembersJson);
            Guests = TeamMeetingSerializer.Guests(m.GuestsJson);
            return Page();
        }

        // New record — must have a valid slot to record against.
        if (!DateOnly.TryParse(date, out var nd) || string.IsNullOrWhiteSpace(area) || !MeetingShifts.IsValid(shift))
        {
            SlotMissing = true;
            return Page();
        }

        MeetingDate = nd.ToString("yyyy-MM-dd");
        Area = area!;
        Shift = shift!;
        ShiftTime = MeetingShifts.TimeFor(shift!);
        // Prefill from the slot.
        Team = area;
        Location = area;
        CompiledOn = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        MeetingDateTime = $"{nd:dd/MM/yyyy} {ShiftTime}".Trim();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string meetingDate, string area, string shift, int? editingId)
    {
        // Recording a team meeting is open to any signed-in user.

        if (!DateOnly.TryParse(meetingDate, out var d) || string.IsNullOrWhiteSpace(area) || !MeetingShifts.IsValid(shift))
            return RedirectToPage("/MeetingRota");

        var user = _users.GetCurrentUser();

        var m = editingId.HasValue
            ? await _db.TeamMeetings.FirstOrDefaultAsync(x => x.Id == editingId.Value)
            : null;
        // No id (or vanished): fall back to the natural key so a reopen updates
        // the shared record instead of inserting a duplicate.
        m ??= await FindByKeyAsync(d, area, shift);

        var isNew = m == null;
        m ??= new TeamMeeting { SubmittedBy = user.Username };

        m.MeetingDate = d;
        m.Area = area;
        m.Shift = shift;
        m.Supervisor = Supervisor; m.CostCentre = CostCentre; m.Team = Team;
        m.CompiledOn = DateOnly.TryParse(CompiledOn, out var co) ? co : null;
        m.Location = Location; m.MeetingDateTime = MeetingDateTime;
        m.Actions = Actions; m.HealthSafety = HealthSafety;
        m.CustomerSatisfaction = CustomerSatisfaction; m.Kpis = Kpis;
        m.EmployeeSatisfaction = EmployeeSatisfaction; m.Aob = Aob;
        m.MeetingResults = MeetingResults; m.MinutesKeeper = MinutesKeeper;
        m.ProblemsJson = TeamMeetingSerializer.NormalizeProblems(ProblemsJson);
        m.TeamMembersJson = TeamMeetingSerializer.NormalizeAttendees(TeamMembersJson);
        m.GroupMembersJson = TeamMeetingSerializer.NormalizeAttendees(GroupMembersJson);
        m.GuestsJson = TeamMeetingSerializer.NormalizeGuests(GuestsJson);
        m.LastEditedBy = user.DisplayName;
        m.LastEditedAt = DateTime.UtcNow;

        if (isNew) _db.TeamMeetings.Add(m);

        // Only a confirmed DB write counts as saved — the PDF (offered on the
        // Success page) is never generated before this succeeds.
        await _db.SaveChangesAsync();

        return RedirectToPage("/Success", new { meetingId = m.Id, saved = true });
    }

    // Continuity lookup: newest matching record for the slot, newest-edited first.
    Task<TeamMeeting?> FindByKeyAsync(DateOnly date, string area, string shift) =>
        _db.TeamMeetings
            .Where(x => x.MeetingDate == date && x.Area == area && x.Shift == shift)
            .OrderByDescending(x => x.LastEditedAt ?? x.SubmittedAt)
            .FirstOrDefaultAsync();
}
