using Microsoft.EntityFrameworkCore;
using TL.Data;

namespace TL.Services;

public class RecordDeleteService
{
    private readonly AppDbContext _db;

    public RecordDeleteService(AppDbContext db) => _db = db;

    /// <summary>
    /// Deletes by kind. For "hod", resolves the real table server-side (HodDailyAudits
    /// first, then legacy Audit pseudo-shifts) — never trusts a client bool alone.
    /// </summary>
    public async Task<bool> DeleteAsync(string kind, int id, bool isNewHodAudit = false)
    {
        return kind switch
        {
            "shifts" or "session" => await DeleteShiftSubmissionAsync(id),
            "hod" => await DeleteHodByIdAsync(id, preferNew: isNewHodAudit),
            "senior" => await DeleteSeniorAuditAsync(id),
            _ => false,
        };
    }

    public async Task<bool> DeleteShiftSubmissionAsync(int id)
    {
        var sub = await _db.ShiftSubmissions.FirstOrDefaultAsync(s => s.Id == id);
        if (sub == null) return false;
        // Never delete a live TL shift via the HoD legacy path by accident — callers that
        // want a normal shift must use kind=shifts. Legacy audit rows only.
        _db.ShiftSubmissions.Remove(sub);
        await _db.SaveChangesAsync();
        return true;
    }

    async Task<bool> DeleteHodByIdAsync(int id, bool preferNew)
    {
        // Prefer the table the UI said, but always fall back to the other so a missing
        // client flag cannot wipe a TL shiftSubmission with the same numeric id.
        if (preferNew)
        {
            if (await DeleteHodAuditAsync(id)) return true;
            return await DeleteLegacyHodPseudoShiftAsync(id);
        }

        if (await DeleteLegacyHodPseudoShiftAsync(id)) return true;
        return await DeleteHodAuditAsync(id);
    }

    async Task<bool> DeleteHodAuditAsync(int id)
    {
        var audit = await _db.HodDailyAudits.FindAsync(id);
        if (audit == null) return false;
        _db.HodDailyAudits.Remove(audit);
        await _db.SaveChangesAsync();
        return true;
    }

    async Task<bool> DeleteLegacyHodPseudoShiftAsync(int id)
    {
        var sub = await _db.ShiftSubmissions.FirstOrDefaultAsync(s =>
            s.Id == id && s.Shift == ShiftQueryExtensions.AuditPseudoShift);
        if (sub == null) return false;
        _db.ShiftSubmissions.Remove(sub);
        await _db.SaveChangesAsync();
        return true;
    }

    async Task<bool> DeleteSeniorAuditAsync(int id)
    {
        var audit = await _db.SeniorWeeklyAudits.FindAsync(id);
        if (audit == null) return false;
        _db.SeniorWeeklyAudits.Remove(audit);
        await _db.SaveChangesAsync();
        return true;
    }
}
