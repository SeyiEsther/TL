namespace TL.Models;

// Official Rittal document/controlled-form numbers printed in each PDF footer.
// Keyed by form type so the numbers can be maintained from the Admin UI without
// a code change.
public class DocumentNumber
{
    public int Id { get; set; }
    public string FormType { get; set; } = "";  // unique key, e.g. "TeamMeeting"
    public string Label { get; set; } = "";      // human name, e.g. "Team Meeting"
    public string Number { get; set; } = "";      // e.g. "CI038/01/1123/CW"
    public int SortOrder { get; set; }
}

public static class DocumentFormTypes
{
    public const string TeamMeeting = "TeamMeeting";

    // Seeded when a row for the type is missing. The Team Meeting is the only
    // controlled form with a document number.
    public static readonly (string Type, string Label, string Number, int Order)[] Defaults =
    [
        (TeamMeeting, "Team Meeting", "CI038/01/1123/CW", 1),
    ];
}
