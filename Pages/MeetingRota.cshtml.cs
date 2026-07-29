using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Services;

namespace TL.Pages;

// The rota schedule itself is computed client-side. The only server data is the
// set of already-recorded team meetings for the current week, so slots can show
// a "recorded" marker and link to the existing record.
public class MeetingRotaModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly PortalAccessService _access;

    public MeetingRotaModel(AppDbContext db, PortalAccessService access)
    {
        _db = db;
        _access = access;
    }

    public bool CanEdit { get; set; }
    public List<RecordedSlot> Recorded { get; set; } = [];

    public async Task OnGetAsync()
    {
        CanEdit = _access.CanAccessHod();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var (weekStart, weekEnd) = WeekMath.Bounds(today);

        try
        {
            Recorded = await _db.TeamMeetings
                .Where(m => m.MeetingDate >= weekStart && m.MeetingDate <= weekEnd)
                .Select(m => new RecordedSlot(m.MeetingDate.ToString("yyyy-MM-dd"), m.Area, m.Shift, m.Id))
                .ToListAsync();
        }
        catch
        {
            Recorded = [];
        }
    }

    public record RecordedSlot(string Date, string Area, string Shift, int Id);
}
