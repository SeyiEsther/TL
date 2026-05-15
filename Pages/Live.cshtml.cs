using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class LiveModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserService _users;

    public LiveModel(AppDbContext db, UserService users)
    {
        _db = db;
        _users = users;
    }

    public DateOnly Today { get; set; }
    public string CurrentShift { get; set; } = "";
    public int ReportingCount { get; set; }
    public Dictionary<string, ShiftSubmission> ByArea { get; set; } = new();
    public Dictionary<int, string> ReasonTexts { get; set; } = new();
    public HashSet<string> PendingAcknowledgement { get; set; } = new();
    public int PendingCount { get; set; }
    public int[] HourYCount { get; set; } = new int[8];
    public int[] HourNCount { get; set; } = new int[8];

    public static string DetectShift()
    {
        var h = DateTime.Now.Hour;
        if (h >= 6 && h < 14) return "Day";
        if (h >= 14 && h < 22) return "Afternoon";
        return "Night";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = _users.GetCurrentUser();
        if (!user.IsManager)
            return RedirectToPage("/Index");

        Today = DateOnly.FromDateTime(DateTime.Today);
        CurrentShift = DetectShift();

        var submissions = await _db.ShiftSubmissions
            .Include(s => s.Hours.OrderBy(h => h.HourNumber))
            .Where(s => s.ShiftDate == Today && s.Shift == CurrentShift)
            .ToListAsync();

        ReportingCount = submissions.Count;

        foreach (var sub in submissions)
        {
            if (sub.Area != null && !ByArea.ContainsKey(sub.Area))
                ByArea[sub.Area] = sub;
        }

        var reasons = await _db.MissedTargetReasons.ToListAsync();
        ReasonTexts = reasons.ToDictionary(r => r.Id, r => r.ReasonText);

        var pendingAreas = await _db.ShiftSubmissions
            .Where(s => s.OutgoingTLSignature != null && s.OutgoingTLSignature != ""
                     && (s.IncomingTLSignature == null || s.IncomingTLSignature == ""))
            .Select(s => s.Area)
            .Distinct()
            .ToListAsync();

        PendingAcknowledgement = new HashSet<string>(pendingAreas.Where(a => a != null).Select(a => a!));
        PendingCount = PendingAcknowledgement.Count;

        HourYCount = new int[8];
        HourNCount = new int[8];
        foreach (var sub in ByArea.Values)
        {
            for (int i = 0; i < 8; i++)
            {
                int hrNum = i + 1;
                var hour = sub.Hours.FirstOrDefault(h => h.HourNumber == hrNum);
                if (hour == null) continue;
                if (hour.HourlyTargetAchieved == true) HourYCount[i]++;
                else if (hour.HourlyTargetAchieved == false) HourNCount[i]++;
            }
        }

        return Page();
    }
}
