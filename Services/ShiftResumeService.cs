using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Services;

public class ShiftResumeService
{
    private readonly AppDbContext _db;

    public ShiftResumeService(AppDbContext db) => _db = db;

    public static string NormalizeTl(string? name) => (name ?? "").Trim();

    public static bool TlEquals(string? a, string? b) =>
        string.Equals(NormalizeTl(a), NormalizeTl(b), StringComparison.OrdinalIgnoreCase);

    /// <summary>Shift still being filled — not signed off by outgoing TL.</summary>
    public static bool IsInProgress(ShiftSubmission s) =>
        string.IsNullOrWhiteSpace(s.OutgoingTLSignature);

    /// <summary>
    /// Find a shift to resume for this date/shift/area.
    /// Prefers in-progress shifts; matches team leader loosely (case/trim).
    /// Falls back to any in-progress shift in the slot (shared PC / same area).
    /// </summary>
    public async Task<ShiftSubmission?> FindForResumeAsync(
        DateOnly date, string shift, string area, string? teamLeader)
    {
        var slot = await _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .Where(s => s.ShiftDate == date && s.Shift == shift && s.Area == area)
            .OrderByDescending(s => s.LastEditedAt ?? s.SubmittedAt)
            .ToListAsync();

        if (slot.Count == 0) return null;

        var inProgress = slot.Where(IsInProgress).ToList();
        if (inProgress.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(teamLeader))
            {
                var tlMatch = inProgress.FirstOrDefault(s => TlEquals(s.TeamLeaderDisplay, teamLeader));
                if (tlMatch != null) return tlMatch;
            }
            return inProgress[0];
        }

        if (!string.IsNullOrWhiteSpace(teamLeader))
            return slot.FirstOrDefault(s => TlEquals(s.TeamLeaderDisplay, teamLeader));

        return null;
    }

    public async Task<ShiftSubmission?> FindPendingHandoverForAreaAsync(
        string area, DateOnly startingDate, string startingShift)
    {
        var candidates = await _db.ShiftSubmissions
            .ExcludeAudits()
            .Where(s => s.Area == area
                && !string.IsNullOrEmpty(s.OutgoingTLSignature)
                && string.IsNullOrEmpty(s.IncomingTLSignature))
            .OrderByDescending(s => s.ShiftDate)
            .ThenByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return candidates.FirstOrDefault(s =>
            !(s.ShiftDate == startingDate && s.Shift == startingShift && IsInProgress(s)));
    }

    public async Task<List<ShiftSubmission>> ListInProgressAsync(DateOnly? onDate = null)
    {
        var q = _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .Where(s => string.IsNullOrEmpty(s.OutgoingTLSignature));

        if (onDate.HasValue)
            q = q.Where(s => s.ShiftDate == onDate.Value);

        return await q
            .OrderByDescending(s => s.LastEditedAt ?? s.SubmittedAt)
            .ToListAsync();
    }
}
