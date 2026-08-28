using TL.Models;

namespace TL.Services;

public class PortalAccessService
{
    private readonly AdminService _admin;
    private readonly UserService _users;
    private readonly PersonListService _people;

    public PortalAccessService(AdminService admin, UserService users, PersonListService people)
    {
        _admin = admin;
        _users = users;
        _people = people;
    }

    public Task EnsureReadyAsync() => _people.EnsureLoadedAsync();

    public bool CanAccessHod() =>
        _admin.IsAdmin()
        || MatchesList(_people.Hods)
        || IsShiftManager();

    public bool CanAccessSenior() =>
        _admin.IsAdmin()
        || MatchesList(_people.Seniors)
        || IsShiftManager();

    // Shift Managers get their own tab. Shift managers keep full access, so
    // full-access users see it too; membership drives their name pickers.
    public bool CanAccessShiftManager() =>
        _admin.IsAdmin()
        || MatchesList(_people.ShiftManagers)
        || IsShiftManager();

    public bool CanAccessManagement() =>
        _admin.IsAdmin() || CanAccessHod() || CanAccessSenior();

    public bool CanAccessPage(string? pagePath) => pagePath switch
    {
        "/HodDashboard" or "/AuditStart" or "/Audit" or "/AuditResult" or "/AuditSuccess" or "/AuditHistory" or "/HodHistory" or "/HodAuditSummary"
            => CanAccessHod(),
        "/SeniorStart" or "/SeniorAudit" or "/SeniorDashboard" or "/SeniorRota" or "/SeniorSuccess" or "/SeniorHistory"
            => CanAccessSenior(),
        "/ShiftManager" or "/ShiftManagerReport" or "/ShiftManagerReportSuccess" or "/ShiftManagerHistory"
            => CanAccessShiftManager(),
        "/History"
            => true, // history is viewable by everyone; delete stays admin-only in the page
        "/Dashboard" or "/ShiftHistory" or "/Today" or "/Actions"
            => CanAccessManagement(),
        "/Admin" or "/AdminTeamLeaders" or "/AdminHodNames" or "/AdminSeniorNames" or "/AdminFullAccess" or "/AdminActions" or "/AdminShiftManagers"
            => _admin.IsAdmin(),
        _ => true,
    };

    /// <summary>
    /// API path gates — keep aligned with page roles so PDF/list endpoints aren't wider than the UI.
    /// </summary>
    public bool CanAccessApi(string? requestPath)
    {
        var path = (requestPath ?? "").ToLowerInvariant();
        if (path.StartsWith("/api/audits/hod"))
            return CanAccessHod();
        if (path.StartsWith("/api/audits/senior"))
            return CanAccessSenior();
        if (path.StartsWith("/api/audits/walkaround") || path.StartsWith("/api/audits/"))
            return CanAccessHod() || CanAccessSenior();
        if (path.StartsWith("/api/submissions"))
            return CanAccessManagement();
        return true;
    }

    bool IsShiftManager() => MatchesList(_people.FullAccess);

    bool MatchesList(IReadOnlyList<string> names)
    {
        var user = _users.GetCurrentUser();
        if (string.IsNullOrWhiteSpace(user.DisplayName) && string.IsNullOrWhiteSpace(user.Username))
            return false;

        return names.Any(n =>
            PortalNameMatcher.Matches(n, user.DisplayName) ||
            PortalNameMatcher.Matches(n, user.Username));
    }
}

public static class PortalNameMatcher
{
    public static bool Matches(string? configured, string? actual)
    {
        var a = Normalize(configured);
        var b = Normalize(actual);
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;

        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            return true;

        if (FullNamesEquivalent(a, b))
            return true;

        if (!b.Contains(' ') && a.Contains(' '))
            return FirstNamesCompatible(b, FirstNameOf(a));

        return false;
    }

    public static string Normalize(string? value) =>
        string.Join(' ', (value ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    static bool FullNamesEquivalent(string configured, string actual)
    {
        if (!configured.Contains(' ') || !actual.Contains(' '))
            return false;

        if (!string.Equals(LastNameOf(configured), LastNameOf(actual), StringComparison.OrdinalIgnoreCase))
            return false;

        return FirstNamesCompatible(FirstNameOf(configured), FirstNameOf(actual));
    }

    static bool FirstNamesCompatible(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            return true;

        if (NicknamesEquivalent(a, b))
            return true;

        var (shorter, longer) = a.Length <= b.Length ? (a, b) : (b, a);
        return shorter.Length >= 3 &&
               longer.StartsWith(shorter, StringComparison.OrdinalIgnoreCase);
    }

    // Common nickname ↔ formal-name pairs. AD often stores the formal name
    // ("Michael") while the picker list uses the everyday name ("Mike"),
    // which plain prefix matching can't bridge (Mich… vs Mike).
    static readonly Dictionary<string, string> NicknameCanonical =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["mike"] = "michael", ["mick"] = "michael", ["micky"] = "michael", ["mickey"] = "michael",
            ["micheal"] = "michael", // very common letter-swap misspelling
            ["nick"] = "nicholas", ["nicky"] = "nicholas", ["nik"] = "nicholas",
            ["tony"] = "anthony",
            ["jim"] = "james", ["jimmy"] = "james", ["jamie"] = "james",
            ["steve"] = "steven", ["stevie"] = "steven",
            ["bob"] = "robert", ["rob"] = "robert", ["bobby"] = "robert", ["robbie"] = "robert",
            ["bill"] = "william", ["will"] = "william", ["billy"] = "william", ["willy"] = "william",
            ["dick"] = "richard", ["rich"] = "richard", ["rick"] = "richard", ["richie"] = "richard",
            ["dave"] = "david",
            ["dan"] = "daniel", ["danny"] = "daniel",
            ["tom"] = "thomas", ["tommy"] = "thomas",
            ["chris"] = "christopher",
            ["matt"] = "matthew",
            ["andy"] = "andrew", ["drew"] = "andrew",
            ["ben"] = "benjamin",
            ["sam"] = "samuel",
            ["joe"] = "joseph", ["joey"] = "joseph",
            ["ed"] = "edward", ["eddie"] = "edward", ["ted"] = "edward",
            ["ken"] = "kenneth", ["kenny"] = "kenneth",
            ["greg"] = "gregory",
            ["pat"] = "patrick", ["paddy"] = "patrick",
            ["tim"] = "timothy", ["timmy"] = "timothy",
            ["ron"] = "ronald", ["ronnie"] = "ronald",
            ["don"] = "donald", ["donnie"] = "donald",
            ["fred"] = "frederick", ["freddie"] = "frederick",
            ["charlie"] = "charles", ["chuck"] = "charles",
            ["jon"] = "jonathan", ["johnny"] = "john", ["jack"] = "john",
            ["alex"] = "alexander", ["sandy"] = "alexander",
            ["phil"] = "philip", ["philip"] = "phillip",
            ["nate"] = "nathan",
            ["gabe"] = "gabriel",
            ["vic"] = "victor",
            ["les"] = "leslie",
            ["si"] = "simon",
        };

    static bool NicknamesEquivalent(string a, string b)
    {
        var ca = NicknameCanonical.GetValueOrDefault(a, a);
        var cb = NicknameCanonical.GetValueOrDefault(b, b);
        return string.Equals(ca, cb, StringComparison.OrdinalIgnoreCase);
    }

    static string FirstNameOf(string fullName)
    {
        var i = fullName.LastIndexOf(' ');
        return i <= 0 ? fullName : fullName[..i];
    }

    static string LastNameOf(string fullName)
    {
        var i = fullName.LastIndexOf(' ');
        return i < 0 ? fullName : fullName[(i + 1)..];
    }
}
