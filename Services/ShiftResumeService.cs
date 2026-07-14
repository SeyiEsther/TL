using System.Collections.Concurrent;
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

    public async Task<ShiftSubmission?> FindForResumeAsync(DateOnly date, string shift, string area)
    {
        var slot = await SlotQuery(date, shift, area).ToListAsync();
        return slot.FirstOrDefault(IsInProgress);
    }

    public async Task<bool> SlotHasClosedAsync(DateOnly date, string shift, string area) =>
        await SlotQuery(date, shift, area).AnyAsync(s =>
            s.OutgoingTLSignature != null && s.OutgoingTLSignature != "");

    // Resolve the open shift for a slot, or create one if none exists — serialised per
    // slot so two concurrent "start shift" requests can't both insert a stub and leave
    // a duplicate open session behind. The second caller waits, then finds the first's
    // committed row instead of creating its own.
    public async Task<SlotResolution> ResolveOrCreateOpenAsync(
        DateOnly date, string shift, string area, Func<ShiftSubmission> createStub)
    {
        var gate = SlotGate.For(date, shift, area);
        await gate.WaitAsync();
        try
        {
            var existing = await FindForResumeAsync(date, shift, area);
            if (existing != null) return SlotResolution.Resumed(existing);

            if (await SlotHasClosedAsync(date, shift, area))
                return SlotResolution.AlreadyClosed();

            var stub = createStub();
            _db.ShiftSubmissions.Add(stub);
            await _db.SaveChangesAsync();
            return SlotResolution.Created(stub);
        }
        finally
        {
            gate.Release();
        }
    }

    IQueryable<ShiftSubmission> SlotQuery(DateOnly date, string shift, string area) =>
        _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .Where(s => s.ShiftDate == date && s.Shift == shift && s.Area == area)
            .OrderByDescending(s => s.Hours.Count)
            .ThenByDescending(s => s.LastEditedAt ?? s.SubmittedAt);
}

public class SlotResolution
{
    public ShiftSubmission? Shift { get; private init; }
    public bool Closed { get; private init; }

    public static SlotResolution Resumed(ShiftSubmission shift) => new() { Shift = shift };
    public static SlotResolution Created(ShiftSubmission shift) => new() { Shift = shift };
    public static SlotResolution AlreadyClosed() => new() { Closed = true };
}

// One lock per (date, shift, area) slot, shared across requests in this process.
static class SlotGate
{
    static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new();

    public static SemaphoreSlim For(DateOnly date, string shift, string area) =>
        Gates.GetOrAdd(
            $"{date:yyyy-MM-dd}|{shift}|{area}".ToLowerInvariant(),
            _ => new SemaphoreSlim(1, 1));
}
