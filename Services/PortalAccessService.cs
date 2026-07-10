using TL.Models;

namespace TL.Services;

/// <summary>Role-based visibility for HoD, Senior, Dashboard, and History.</summary>
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

    public bool CanAccessHod() => _admin.IsAdmin() || MatchesList(_people.Hods);

    public bool CanAccessSenior() => _admin.IsAdmin() || MatchesList(_people.Seniors);

    /// <summary>Factory-wide dashboards and full history — not for general team leaders.</summary>
    public bool CanAccessManagement() =>
        _admin.IsAdmin() || CanAccessHod() || CanAccessSenior();

    public bool CanAccessPage(string? pagePath) => pagePath switch
    {
        "/HodDashboard" or "/AuditStart" or "/Audit" or "/AuditResult" or "/AuditSuccess" or "/AuditHistory"
            => CanAccessHod(),
        "/SeniorStart" or "/SeniorAudit" or "/SeniorDashboard" or "/SeniorRota" or "/SeniorSuccess"
            => CanAccessSenior(),
        "/Dashboard" or "/History" => CanAccessManagement(),
        "/Admin" => _admin.IsAdmin(),
        _ => true,
    };

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
        return !string.IsNullOrEmpty(a) &&
               string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string? value) =>
        string.Join(' ', (value ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
