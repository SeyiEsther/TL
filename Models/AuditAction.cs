namespace TL.Models;

// A structured, owned action raised from an audit finding — the unit that
// carries the PDCA loop. Stored in its own table (queried across audits by
// owner, updated independently), NOT as JSON inside an audit.
public class AuditAction
{
    public int Id { get; set; }

    // Polymorphic soft-link to the source audit (there is no single audit table).
    public string SourceType { get; set; } = "";     // ActionSourceTypes.*
    public int? SourceId { get; set; }
    // Denormalised so the worklist renders without joining four audit tables.
    public string? SourceLabel { get; set; }
    public string? AuditType { get; set; }
    public string? Area { get; set; }
    public DateOnly? AuditDate { get; set; }

    public string Text { get; set; } = "";

    public string RaisedByName { get; set; } = "";
    public string RaisedByUsername { get; set; } = "";
    public DateTime RaisedAt { get; set; } = DateTime.UtcNow;

    public string OwnerName { get; set; } = "";
    // Normalised owner name for fuzzy matching against the logged-in user.
    public string OwnerKey { get; set; } = "";
    // True for shared destinations (e.g. Maintenance) that no individual claims.
    public bool OwnerIsExternal { get; set; }

    public DateOnly? DueDate { get; set; }

    public string Status { get; set; } = ActionStatus.Open;   // Open | Complete
    public DateTime? CompletedAt { get; set; }
    public string? CompletedByName { get; set; }
    public string? CompletionNote { get; set; }
}

public static class ActionStatus
{
    public const string Open = "Open";
    public const string Complete = "Complete";
}

public static class ActionSourceTypes
{
    public const string HodDaily = "HodDaily";
    public const string SeniorWeekly = "SeniorWeekly";
    public const string Manual = "Manual";

    public static string Label(string? t) => t switch
    {
        HodDaily => "HOD Daily Audit",
        SeniorWeekly => "Senior Weekly Audit",
        Manual => "Manual",
        _ => t ?? "",
    };
}

// Shared/external owner destinations — no individual logs in to claim these.
// Seeded into the ActionOwner picker list and flagged external on the action.
public static class ActionOwners
{
    public static readonly string[] External = ["Maintenance"];

    public static bool IsExternal(string? ownerName) =>
        ownerName != null && External.Any(e => string.Equals(e, ownerName.Trim(), StringComparison.OrdinalIgnoreCase));
}
