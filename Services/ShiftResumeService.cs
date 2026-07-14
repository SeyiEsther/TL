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

    public static bool IsClosed(ShiftSubmission s) => !IsInProgress(s);

    public async Task<ShiftSubmission?> FindForResumeAsync(
        DateOnly date, string shift, string area, string? teamLeader = null)
    {
        var slot = await SlotQuery(date, shift, area).ToListAsync();
        return slot.FirstOrDefault(IsInProgress);
    }

    public async Task<bool> SlotHasClosedAsync(DateOnly date, string shift, string area) =>
        await SlotQuery(date, shift, area).AnyAsync(s =>
            s.OutgoingTLSignature != null && s.OutgoingTLSignature != "");

    IQueryable<ShiftSubmission> SlotQuery(DateOnly date, string shift, string area) =>
        _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .Where(s => s.ShiftDate == date && s.Shift == shift && s.Area == area)
            .OrderByDescending(s => s.Hours.Count)
            .ThenByDescending(s => s.LastEditedAt ?? s.SubmittedAt);
}
