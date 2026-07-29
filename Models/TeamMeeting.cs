namespace TL.Models;

// Rittal Team Meeting (doc CI038/01/1123/CW). Separate from the audit tables.
// Natural key for continuity: MeetingDate + Area + Shift (no user field).
public class TeamMeeting
{
    public int Id { get; set; }

    public DateOnly MeetingDate { get; set; }
    public string Area { get; set; } = "";
    public string Shift { get; set; } = ""; // Nightshift | Dayshift | Backshift

    // Header
    public string? Supervisor { get; set; }
    public string? CostCentre { get; set; }
    public string? Team { get; set; }
    public DateOnly? CompiledOn { get; set; }
    public string? Location { get; set; }
    public string? MeetingDateTime { get; set; }

    // Meeting content — free text, all optional
    public string? Actions { get; set; }
    public string? HealthSafety { get; set; }
    public string? CustomerSatisfaction { get; set; }
    public string? Kpis { get; set; }
    public string? EmployeeSatisfaction { get; set; }
    public string? Aob { get; set; }

    public string? MeetingResults { get; set; }
    public string? MinutesKeeper { get; set; }

    // Repeatable sections stored as JSON (same pattern as audit AnswersJson)
    public string? ProblemsJson { get; set; }
    public string? TeamMembersJson { get; set; }
    public string? GroupMembersJson { get; set; }
    public string? GuestsJson { get; set; }

    // Metadata
    public string SubmittedBy { get; set; } = "";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string? LastEditedBy { get; set; }
    public DateTime? LastEditedAt { get; set; }
}

public class TeamMeetingProblem
{
    public string? Problem { get; set; }
    public string? Action { get; set; }
    public string? Responsible { get; set; }
    public DateOnly? FinishDate { get; set; }
    public int Status { get; set; } // 0–4
}

public class TeamMeetingAttendee
{
    public string? Name { get; set; }
    public bool Present { get; set; }
}

public class TeamMeetingGuest
{
    public string? Name { get; set; }
    public string? Department { get; set; }
    public string? Telephone { get; set; }
}

public static class TeamMeetingStatus
{
    public static readonly (int Value, string Label)[] All =
    [
        (0, "0 — Problem identified"),
        (1, "1 — Owner named"),
        (2, "2 — Solution in process"),
        (3, "3 — Solution implemented"),
        (4, "4 — Standardised & sustainability assured"),
    ];

    public static string Label(int v) => All.FirstOrDefault(x => x.Value == v).Label ?? v.ToString();
    public static string Short(int v) => v is >= 0 and <= 4 ? v.ToString() : "0";
}

public static class MeetingShifts
{
    public const string Night = "Nightshift";
    public const string Day = "Dayshift";
    public const string Back = "Backshift";

    public static readonly string[] All = [Night, Day, Back];
    public static bool IsValid(string? s) => s != null && All.Contains(s);

    public static string TimeFor(string? shift) => shift switch
    {
        Night => "06:45",
        Day => "08:00",
        Back => "15:15",
        _ => "",
    };
}
