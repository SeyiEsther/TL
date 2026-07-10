namespace TL.Models;

public class PickerPerson
{
    public int Id { get; set; }
    /// <summary>TeamLeader or Hod</summary>
    public string ListKind { get; set; } = "";
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public static class PersonListKinds
{
    public const string TeamLeader = "TeamLeader";
    public const string Hod = "Hod";
}
