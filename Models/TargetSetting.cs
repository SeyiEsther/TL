namespace TL.Models;

// An editable production target, stored so admins can change it from the UI
// without a redeploy. Keyed by a stable code (TargetKeys.*), not by row order,
// so reads never depend on insert sequence.
public class TargetSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = "";   // TargetKeys.*
    public int Value { get; set; }

    // Audit trail: who last changed this target and when (item 3 requirement).
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public static class TargetKeys
{
    public const string Shift = "ShiftTarget";
    public const string Day = "DayTarget";
    public const string Week = "WeekTarget";

    // Built-in defaults — the values that were hard-coded on the dashboard before
    // targets moved into the database. Used to seed the table on first run.
    public static readonly IReadOnlyDictionary<string, (string Label, int Default)> Definitions =
        new Dictionary<string, (string, int)>
        {
            [Shift] = ("Forms per shift", 35),
            [Day] = ("Forms per day", 105),
            [Week] = ("Forms per week", 315),
        };
}
