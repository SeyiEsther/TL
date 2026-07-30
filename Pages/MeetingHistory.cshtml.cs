using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Services;

namespace TL.Pages;

// Recorded team meetings, grouped into the same Week N + A/B cycle buckets the
// Meeting Rota calendar uses, so the history reads like the rota. Viewable by
// everyone; recording/editing stays HoD-gated on the session page itself.
public class MeetingHistoryModel : PageModel
{
    // Matches the Meeting Rota default cycle start (Mon 03 Aug 2026 = Week A).
    private static readonly DateOnly CycleStart = new(2026, 8, 3);

    private readonly AppDbContext _db;
    public MeetingHistoryModel(AppDbContext db) => _db = db;

    public List<WeekGroup> Weeks { get; set; } = [];
    public int TotalMeetings { get; set; }

    public async Task OnGetAsync()
    {
        List<MeetingRow> rows;
        try
        {
            rows = await _db.TeamMeetings
                .OrderByDescending(m => m.MeetingDate)
                .Select(m => new MeetingRow(
                    m.Id, m.MeetingDate, m.Area, m.Shift, m.LastEditedBy ?? m.SubmittedBy,
                    m.LastEditedAt ?? m.SubmittedAt))
                .ToListAsync();
        }
        catch
        {
            rows = [];
        }

        TotalMeetings = rows.Count;

        Weeks = rows
            .GroupBy(r => WeekMath.Bounds(r.Date).Start)
            .OrderByDescending(g => g.Key)
            .Select(g => new WeekGroup(
                WeekMath.IsoWeekNumber(g.Key),
                g.Key,
                WeekMath.Bounds(g.Key).End,
                CycleFor(g.Key),
                g.OrderBy(r => r.Date).ThenBy(r => r.Area).ThenBy(r => r.Shift).ToList()))
            .ToList();
    }

    // Whole weeks between this week's Monday and the cycle-start Monday: even = A, odd = B.
    static string CycleFor(DateOnly weekMonday)
    {
        var cycleMonday = WeekMath.Bounds(CycleStart).Start;
        var weeks = (weekMonday.DayNumber - cycleMonday.DayNumber) / 7;
        return (((weeks % 2) + 2) % 2) == 0 ? "A" : "B";
    }

    public record MeetingRow(int Id, DateOnly Date, string Area, string Shift, string By, DateTime When);

    public record WeekGroup(int WeekNumber, DateOnly Start, DateOnly End, string Cycle, List<MeetingRow> Meetings);
}
