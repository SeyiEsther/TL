using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;

namespace TL.Pages;

public class TodayModel : PageModel
{
    private readonly AppDbContext _db;
    public TodayModel(AppDbContext db) => _db = db;

    public List<TodayShift> Shifts { get; set; } = new();

    [BindProperty] public string? IncomingSignature { get; set; }

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rows = await _db.ShiftSubmissions
            .Include(s => s.Hours)
            .Where(s => s.ShiftDate == today)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        Shifts = rows.Select(s => new TodayShift
        {
            Id = s.Id,
            Shift = s.Shift,
            Area = s.Area,
            TeamLeaderDisplay = s.TeamLeaderDisplay,
            HoursCompleted = s.HoursCompleted,
            SubmittedAt = s.SubmittedAt,
            LastEditedBy = s.LastEditedBy,
            LastEditedAt = s.LastEditedAt,
            Escalations = s.Escalations,
            OutgoingTLSignature = s.OutgoingTLSignature,
            IncomingTLSignature = s.IncomingTLSignature,
            SafetyStatus = s.Hours.Where(h => h.OverallSafetyStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallSafetyStatus).FirstOrDefault(),
            QualityStatus = s.Hours.Where(h => h.OverallQualityStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallQualityStatus).FirstOrDefault(),
            PerfStatus = s.Hours.Where(h => h.OverallPerfStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallPerfStatus).FirstOrDefault(),
        }).ToList();
    }

    public async Task<IActionResult> OnPostSignAsync(int id)
    {
        var sub = await _db.ShiftSubmissions.FindAsync(id);
        if (sub != null && !string.IsNullOrWhiteSpace(IncomingSignature))
        {
            sub.IncomingTLSignature = IncomingSignature.Trim();
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };

    public class TodayShift
    {
        public int Id { get; set; }
        public string Shift { get; set; } = "";
        public string? Area { get; set; }
        public string TeamLeaderDisplay { get; set; } = "";
        public int HoursCompleted { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? LastEditedBy { get; set; }
        public DateTime? LastEditedAt { get; set; }
        public string? Escalations { get; set; }
        public string? OutgoingTLSignature { get; set; }
        public string? IncomingTLSignature { get; set; }
        public string? SafetyStatus { get; set; }
        public string? QualityStatus { get; set; }
        public string? PerfStatus { get; set; }
    }
}
