using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Pages;

public class HandoverModel : PageModel
{
    private readonly AppDbContext _db;
    public HandoverModel(AppDbContext db) => _db = db;

    public string? SelectedArea { get; set; }
    public List<HandoverRecord> Handovers { get; set; } = new();

    public async Task OnGetAsync(string? area)
    {
        SelectedArea = area;
        if (string.IsNullOrEmpty(area)) return;

        // Load all shifts for this area that have an outgoing signature (i.e., completed handovers),
        // plus any pending ones (outgoing set, incoming not yet set)
        var rows = await _db.ShiftSubmissions
            .Where(s => s.Area == area && !string.IsNullOrEmpty(s.OutgoingTLSignature))
            .OrderByDescending(s => s.ShiftDate)
            .ThenByDescending(s => s.SubmittedAt)
            .Include(s => s.Hours)
            .ToListAsync();

        Handovers = rows.Select(s => new HandoverRecord
        {
            Id = s.Id,
            ShiftDate = s.ShiftDate,
            Shift = s.Shift,
            Area = s.Area,
            TeamLeaderDisplay = s.TeamLeaderDisplay,
            HoursCompleted = s.HoursCompleted,
            SubmittedAt = s.SubmittedAt,
            OutgoingTLSignature = s.OutgoingTLSignature,
            IncomingTLSignature = s.IncomingTLSignature,
            IncomingTLSignedAt = s.IncomingTLSignedAt,
            SafetyStatus = s.Hours.Where(h => h.OverallSafetyStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallSafetyStatus).FirstOrDefault(),
            QualityStatus = s.Hours.Where(h => h.OverallQualityStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallQualityStatus).FirstOrDefault(),
            PerfStatus = s.Hours.Where(h => h.OverallPerfStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallPerfStatus).FirstOrDefault(),
        }).ToList();
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };

    public class HandoverRecord
    {
        public int Id { get; set; }
        public DateOnly ShiftDate { get; set; }
        public string Shift { get; set; } = "";
        public string? Area { get; set; }
        public string TeamLeaderDisplay { get; set; } = "";
        public int HoursCompleted { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? OutgoingTLSignature { get; set; }
        public string? IncomingTLSignature { get; set; }
        public DateTime? IncomingTLSignedAt { get; set; }
        public string? SafetyStatus { get; set; }
        public string? QualityStatus { get; set; }
        public string? PerfStatus { get; set; }
        public bool IsPending => string.IsNullOrEmpty(IncomingTLSignature);
        public TimeSpan PendingFor => DateTime.UtcNow - SubmittedAt;
    }
}
