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
    /// Find an in-progress shift to resume for this date/shift/area and team leader.
    /// Never returns another TL's shift — each leader gets their own record.
    /// </summary>
    public async Task<ShiftSubmission?> FindForResumeAsync(
        DateOnly date, string shift, string area, string? teamLeader)
    {
        var tlName = NormalizeTl(teamLeader);
        if (string.IsNullOrWhiteSpace(tlName))
            return null;

        var slot = await _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .Where(s => s.ShiftDate == date && s.Shift == shift && s.Area == area)
            .OrderByDescending(s => s.LastEditedAt ?? s.SubmittedAt)
            .ToListAsync();

        var matches = slot
            .Where(s => IsInProgress(s) && TlEquals(s.TeamLeaderDisplay, tlName))
            .OrderByDescending(s => s.Hours.Count)
            .ThenByDescending(s => s.LastEditedAt ?? s.SubmittedAt)
            .ToList();

        return matches.FirstOrDefault();
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
}
