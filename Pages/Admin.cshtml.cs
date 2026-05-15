using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class AdminModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserService _users;

    public AdminModel(AppDbContext db, UserService users)
    {
        _db = db;
        _users = users;
    }

    public List<MissedTargetReason> Reasons { get; set; } = new();

    [BindProperty] public string NewReasonText { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_users.GetCurrentUser().IsManager) return RedirectToPage("/Index");

        Reasons = await _db.MissedTargetReasons
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.ReasonText)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!_users.GetCurrentUser().IsManager) return RedirectToPage("/Index");

        if (!string.IsNullOrWhiteSpace(NewReasonText))
        {
            var maxOrder = await _db.MissedTargetReasons.MaxAsync(r => (int?)r.SortOrder) ?? 0;
            _db.MissedTargetReasons.Add(new MissedTargetReason
            {
                ReasonText = NewReasonText.Trim(),
                SortOrder = maxOrder + 10,
                IsActive = true
            });
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!_users.GetCurrentUser().IsManager) return RedirectToPage("/Index");

        var reason = await _db.MissedTargetReasons.FindAsync(id);
        if (reason != null)
        {
            // Check if used in any existing records — deactivate rather than hard delete
            var inUse = await _db.HourlyChecks.AnyAsync(h => h.MissedTargetReasonId == id);
            if (inUse)
                reason.IsActive = false;
            else
                _db.MissedTargetReasons.Remove(reason);

            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        if (!_users.GetCurrentUser().IsManager) return RedirectToPage("/Index");

        var reason = await _db.MissedTargetReasons.FindAsync(id);
        if (reason != null)
        {
            reason.IsActive = true;
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
