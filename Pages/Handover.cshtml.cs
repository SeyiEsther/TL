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
    public List<HandoverRecord> Records { get; set; } = new();

    public class HandoverRecord
    {
        public int Id;
        public DateOnly ShiftDate;
        public string Shift = "";
        public string TeamLeaderDisplay = "";
        public string? OutgoingTLSignature;
        public DateTime SubmittedAt;
        public string? IncomingTLSignature;
        public DateTime? IncomingTLSignedAt;
        public bool IsPending => !string.IsNullOrEmpty(OutgoingTLSignature) && string.IsNullOrEmpty(IncomingTLSignature);
    }

    public async Task OnGetAsync(string? area)
    {
        SelectedArea = area;
        if (string.IsNullOrEmpty(area)) return;

        var submissions = await _db.ShiftSubmissions
            .Where(s => s.Area == area)
            .OrderByDescending(s => s.ShiftDate)
            .ThenByDescending(s => s.SubmittedAt)
            .Select(s => new HandoverRecord
            {
                Id = s.Id,
                ShiftDate = s.ShiftDate,
                Shift = s.Shift,
                TeamLeaderDisplay = s.TeamLeaderDisplay,
                OutgoingTLSignature = s.OutgoingTLSignature,
                SubmittedAt = s.SubmittedAt,
                IncomingTLSignature = s.IncomingTLSignature,
                IncomingTLSignedAt = s.IncomingTLSignedAt,
            })
            .ToListAsync();

        // Pending (outgoing signed, incoming not signed) at top, then by date desc
        Records = submissions
            .OrderByDescending(r => r.IsPending)
            .ThenByDescending(r => r.ShiftDate)
            .ThenByDescending(r => r.SubmittedAt)
            .ToList();
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };
}
