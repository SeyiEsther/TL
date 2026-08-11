using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Services;

// All reads/writes for the structured actions (PDCA) system. Owner matching
// reuses PortalNameMatcher so it behaves like the rest of the app.
public class ActionService
{
    private readonly AppDbContext _db;
    private readonly UserService _users;

    public ActionService(AppDbContext db, UserService users)
    {
        _db = db;
        _users = users;
    }

    // ---- Creating actions from an audit ----------------------------------

    // Inserts the newly-assigned actions for a saved audit, skipping any that
    // already exist open for the same source (so re-submitting doesn't dupe).
    public async Task CreateFromAuditAsync(
        string sourceType, int sourceId, string sourceLabel, string? auditType,
        string? area, DateOnly auditDate, IEnumerable<ActionSerializer.NewAction> rows)
    {
        var user = _users.GetCurrentUser();
        var existing = await _db.AuditActions
            .Where(a => a.SourceType == sourceType && a.SourceId == sourceId)
            .Select(a => new { a.OwnerKey, a.Text, a.Status })
            .ToListAsync();

        var added = false;
        foreach (var r in rows)
        {
            var text = (r.Text ?? "").Trim();
            var owner = (r.Owner ?? "").Trim();
            if (text.Length == 0 || owner.Length == 0) continue;

            var key = PortalNameMatcher.Normalize(owner);
            // Skip a duplicate of an action already open for this audit.
            if (existing.Any(e => e.OwnerKey == key && e.Text == text && e.Status == ActionStatus.Open))
                continue;

            DateOnly? due = DateOnly.TryParse(r.Due, out var d) ? d : null;

            _db.AuditActions.Add(new AuditAction
            {
                SourceType = sourceType,
                SourceId = sourceId,
                SourceLabel = sourceLabel,
                AuditType = auditType,
                Area = area,
                AuditDate = auditDate,
                Text = text,
                RaisedByName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName,
                RaisedByUsername = user.Username,
                RaisedAt = DateTime.UtcNow,
                OwnerName = owner,
                OwnerKey = key,
                OwnerIsExternal = ActionOwners.IsExternal(owner),
                Status = ActionStatus.Open,
            });
            added = true;
        }
        if (added) await _db.SaveChangesAsync();
    }

    // ---- Queries ----------------------------------------------------------

    public Task<List<AuditAction>> AllAsync() =>
        _db.AuditActions.OrderByDescending(a => a.RaisedAt).ToListAsync();

    // Open actions assigned to this user (individual, non-external owners).
    public async Task<List<AuditAction>> OpenForUserAsync(string displayName)
    {
        var open = await _db.AuditActions
            .Where(a => a.Status == ActionStatus.Open && !a.OwnerIsExternal)
            .OrderBy(a => a.DueDate ?? DateOnly.MaxValue)
            .ThenByDescending(a => a.RaisedAt)
            .ToListAsync();
        return open.Where(a => PortalNameMatcher.Matches(a.OwnerName, displayName)).ToList();
    }

    public async Task<int> OpenCountForUserAsync(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return 0;
        return (await OpenForUserAsync(displayName)).Count;
    }

    public Task<List<AuditAction>> RaisedByUserAsync(string username, string displayName) =>
        _db.AuditActions
            .Where(a => a.RaisedByUsername == username
                        || (displayName != "" && a.RaisedByName == displayName))
            .OrderByDescending(a => a.RaisedAt)
            .ToListAsync();

    public Task<AuditAction?> FindAsync(int id) =>
        _db.AuditActions.FirstOrDefaultAsync(a => a.Id == id);

    // ---- Owner resolution (safety net) -----------------------------------

    // Names the app knows about, for deciding whether an owner "resolves".
    public async Task<HashSet<string>> KnownNamesAsync()
    {
        var names = await _db.PickerPersons.Select(p => p.Name).ToListAsync();
        return new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
    }

    // A non-external action whose owner matches no known person is "unresolved"
    // (a likely name-mismatch that would otherwise sit on nobody's list).
    public static bool IsUnresolved(AuditAction a, IEnumerable<string> knownNames) =>
        !a.OwnerIsExternal
        && !knownNames.Any(n => PortalNameMatcher.Matches(a.OwnerName, n));

    // ---- Mutations --------------------------------------------------------

    public const string NoteRequiredMessage =
        "Please describe what was done to complete this action.";

    // Returns (ok, error). Completion note is mandatory and must be real text.
    public async Task<(bool Ok, string? Error)> CompleteAsync(int id, string byName, string? note)
    {
        var clean = (note ?? "").Trim();
        if (clean.Length < 3)
            return (false, NoteRequiredMessage);

        var a = await FindAsync(id);
        if (a == null) return (false, "Action not found.");
        if (a.Status == ActionStatus.Complete) return (true, null);

        a.Status = ActionStatus.Complete;
        a.CompletedAt = DateTime.UtcNow;
        a.CompletedByName = string.IsNullOrWhiteSpace(byName) ? "Unknown" : byName;
        a.CompletionNote = clean;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> ReassignAsync(int id, string newOwner, string byName)
    {
        var a = await FindAsync(id);
        if (a == null || string.IsNullOrWhiteSpace(newOwner)) return false;
        var owner = newOwner.Trim();
        a.OwnerName = owner;
        a.OwnerKey = PortalNameMatcher.Normalize(owner);
        a.OwnerIsExternal = ActionOwners.IsExternal(owner);
        // Reassignment is recorded on the action itself for the audit trail.
        a.CompletionNote = AppendTrail(a.CompletionNote, $"Reassigned to {owner} by {byName} on {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC");
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReopenAsync(int id, string byName)
    {
        var a = await FindAsync(id);
        if (a == null || a.Status != ActionStatus.Complete) return false;
        a.Status = ActionStatus.Open;
        a.CompletionNote = AppendTrail(a.CompletionNote, $"Reopened by {byName} on {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC");
        a.CompletedAt = null;
        a.CompletedByName = null;
        await _db.SaveChangesAsync();
        return true;
    }

    static string AppendTrail(string? existing, string line) =>
        string.IsNullOrWhiteSpace(existing) ? $"[{line}]" : $"{existing}\n[{line}]";
}
