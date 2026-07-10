using Microsoft.EntityFrameworkCore;
using TL.Data;

namespace TL.Services;

public class RecordDeleteService
{
    private readonly AppDbContext _db;

    public RecordDeleteService(AppDbContext db) => _db = db;

    public Task<bool> DeleteAsync(string kind, int id, bool isNewHodAudit) =>
        kind switch
        {
            "shifts" or "handovers" or "session" => DeleteShiftSubmissionAsync(id),
            "hod" when isNewHodAudit => DeleteHodAuditAsync(id),
            "hod" => DeleteShiftSubmissionAsync(id),
            "senior" => DeleteSeniorAuditAsync(id),
            _ => Task.FromResult(false),
        };

    public async Task<bool> DeleteShiftSubmissionAsync(int id)
    {
        var sub = await _db.ShiftSubmissions.FirstOrDefaultAsync(s => s.Id == id);
        if (sub == null) return false;
        _db.ShiftSubmissions.Remove(sub);
        await _db.SaveChangesAsync();
        return true;
    }

    async Task<bool> DeleteHodAuditAsync(int id)
    {
        var audit = await _db.HodDailyAudits.FindAsync(id);
        if (audit == null) return false;
        _db.HodDailyAudits.Remove(audit);
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
