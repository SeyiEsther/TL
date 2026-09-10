namespace TL.Models;

public class TargetSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public int Value { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public static class TargetKeys
{
    public const string Shift = "ShiftTarget";
    public const string Day = "DayTarget";
    public const string Week = "WeekTarget";

    public static readonly IReadOnlyDictionary<string, (string Label, int Default)> Definitions =
        new Dictionary<string, (string, int)>
        {
            [Shift] = ("Forms per shift", 35),
            [Day] = ("Forms per day", 105),
            [Week] = ("Forms per week", 315),
        };
}
