namespace TL.Models;

public class PickerPerson
{
    public int Id { get; set; }
    public string ListKind { get; set; } = "";
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public static class PersonListKinds
{
    public const string TeamLeader = "TeamLeader";
    public const string Hod = "Hod";
    public const string Senior = "Senior";
    public const string FullAccess = "FullAccess";
    public const string ActionOwner = "ActionOwner";
    public const string ShiftManager = "ShiftManager";
}
