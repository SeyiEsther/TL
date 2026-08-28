namespace TL.Models;

// Shift Managers — the shopfloor shift leads who run daily shifts and their own
// audits + daily report. Split out of the combined HOD/full-access section.
// Seeded from here; editable in Admin. Membership does not change access
// (shift managers keep full access via the FullAccess list) — it drives the
// Shift Manager auditor/name pickers.
public static class ShiftManagerRoleList
{
    public static readonly string[] Names =
    [
        "Nicky Gleeson",
        "Vic Ward",
        "Simon Graham",
        "John Fisher",
        "Jonathan Maynard",
        "Dean Campbell",
        "Steven Hawkins",
        "Glen Atkinson",
        "Kyle Anderson",
        "Jim Gray",
        "Steven White",
    ];
}
