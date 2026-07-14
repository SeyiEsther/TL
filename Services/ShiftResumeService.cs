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

    public static bool IsInProgress(ShiftSubmission s) =>
        string.IsNullOrWhiteSpace(s.OutgoingTLSignature);

    // One shift per date/shift/area slot — always resume that row (even after sign-off)
    // so Home → Start does not create duplicates. Team leader name is not used for matching.
    public async Task<ShiftSubmission?> FindForResumeAsync(
        DateOnly date, string shift, string area, string? teamLeader = null)
    {
        return await _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .Where(s => s.ShiftDate == date && s.Shift == shift && s.Area == area)
            .OrderByDescending(s => s.Hours.Count)
            .ThenByDescending(s => s.LastEditedAt ?? s.SubmittedAt)
            .FirstOrDefaultAsync();
    }
}
