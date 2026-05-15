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
    public List<AdminUser> Managers { get; set; } = new();
    public string CurrentUsername { get; set; } = "";

    [BindProperty] public string NewReasonText { get; set; } = "";
    [BindProperty] public string NewManagerUsername { get; set; } = "";
    [BindProperty] public string NewManagerDisplayName { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        var user = _users.GetCurrentUser();
        if (!user.IsManager) return RedirectToPage("/Index");
        CurrentUsername = user.Username;

        Reasons = await _db.MissedTargetReasons
            .OrderBy(r => r.SortOrder).ThenBy(r => r.ReasonText)
            .ToListAsync();

        Managers = await _db.AdminUsers
            .OrderBy(u => u.Username)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAddReasonAsync()
    {
        var user = _users.GetCurrentUser();
        if (!user.IsManager) return RedirectToPage("/Index");

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

    public async Task<IActionResult> OnPostDeleteReasonAsync(int id)
    {
        if (!_users.GetCurrentUser().IsManager) return RedirectToPage("/Index");

        var reason = await _db.MissedTargetReasons.FindAsync(id);
        if (reason != null)
        {
            var inUse = await _db.HourlyChecks.AnyAsync(h => h.MissedTargetReasonId == id);
            if (inUse)
                reason.IsActive = false;
            else
                _db.MissedTargetReasons.Remove(reason);

            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRestoreReasonAsync(int id)
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

    public async Task<IActionResult> OnPostAddManagerAsync()
    {
        var currentUser = _users.GetCurrentUser();
        if (!currentUser.IsManager) return RedirectToPage("/Index");

        var uname = NewManagerUsername.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(uname))
        {
            var exists = await _db.AdminUsers.AnyAsync(u => u.Username == uname);
            if (!exists)
            {
                _db.AdminUsers.Add(new AdminUser
                {
                    Username = uname,
                    DisplayName = string.IsNullOrWhiteSpace(NewManagerDisplayName) ? null : NewManagerDisplayName.Trim(),
                    AddedAt = DateTime.UtcNow,
                    AddedBy = currentUser.Username
                });
                await _db.SaveChangesAsync();
            }
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveManagerAsync(int id)
    {
        var currentUser = _users.GetCurrentUser();
        if (!currentUser.IsManager) return RedirectToPage("/Index");

        var manager = await _db.AdminUsers.FindAsync(id);
        // Prevent removing yourself
        if (manager != null && !string.Equals(manager.Username, currentUser.Username, StringComparison.OrdinalIgnoreCase))
        {
            _db.AdminUsers.Remove(manager);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
