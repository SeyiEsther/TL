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

    public bool CanAccessManagement() =>
        _admin.IsAdmin() || CanAccessHod() || CanAccessSenior();

    public bool CanAccessPage(string? pagePath) => pagePath switch
    {
        "/HodDashboard" or "/AuditStart" or "/Audit" or "/AuditResult" or "/AuditSuccess" or "/AuditHistory" or "/HodHistory"
            => CanAccessHod(),
        "/SeniorStart" or "/SeniorAudit" or "/SeniorDashboard" or "/SeniorRota" or "/SeniorSuccess" or "/SeniorHistory"
            => CanAccessSenior(),
        "/Dashboard" or "/ShiftHistory" or "/History"
            => CanAccessManagement(),
        "/Admin" or "/AdminTeamLeaders" or "/AdminHodNames" or "/AdminSeniorNames"
            => _admin.IsAdmin(),
        _ => true,
    };

    bool IsShiftManager() => MatchesList(ShiftManagerList.Names);

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

        var (shorter, longer) = a.Length <= b.Length ? (a, b) : (b, a);
        return shorter.Length >= 3 &&
               longer.StartsWith(shorter, StringComparison.OrdinalIgnoreCase);
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
