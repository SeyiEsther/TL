using System.Text.Json;
using TL.Models;

namespace TL.Services;

public static class TeamMeetingSerializer
{
    static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    static List<T> Parse<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<T>>(json, Options) ?? []; }
        catch { return []; }
    }

    static string ToJson<T>(IEnumerable<T> items) => JsonSerializer.Serialize(items, Options);

    public static List<TeamMeetingProblem> Problems(string? json) =>
        Parse<TeamMeetingProblem>(json).Where(p =>
            !string.IsNullOrWhiteSpace(p.Problem) || !string.IsNullOrWhiteSpace(p.Action) ||
            !string.IsNullOrWhiteSpace(p.Responsible)).Take(3).ToList();

    public static List<TeamMeetingAttendee> Attendees(string? json) =>
        Parse<TeamMeetingAttendee>(json).Where(a => !string.IsNullOrWhiteSpace(a.Name)).ToList();

    public static List<TeamMeetingGuest> Guests(string? json) =>
        Parse<TeamMeetingGuest>(json).Where(g =>
            !string.IsNullOrWhiteSpace(g.Name) || !string.IsNullOrWhiteSpace(g.Department)).ToList();

    public static string ProblemsToJson(IEnumerable<TeamMeetingProblem> p) => ToJson(p);
    public static string AttendeesToJson(IEnumerable<TeamMeetingAttendee> a) => ToJson(a);
    public static string GuestsToJson(IEnumerable<TeamMeetingGuest> g) => ToJson(g);

    // Re-normalise a raw JSON string coming from the browser: parse to typed,
    // filter empties, re-serialise — so we never store malformed junk.
    public static string NormalizeProblems(string? json) => ProblemsToJson(Problems(json));
    public static string NormalizeAttendees(string? json) => AttendeesToJson(Attendees(json));
    public static string NormalizeGuests(string? json) => GuestsToJson(Guests(json));
}
