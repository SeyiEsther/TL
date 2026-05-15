using System.DirectoryServices.AccountManagement;
using TL.Models;

namespace TL.Services
{
    public class UserService
    {
        // Usernames (without domain) permitted to access the Admin page
        private static readonly HashSet<string> AdminUsernames = new(StringComparer.OrdinalIgnoreCase)
        {
            "kgwynjones",   // K Gwyn Jones
            "ljaworski",    // L Jaworski
            "oogunbayo",    // O Ogunbayo
        };

        private readonly IHttpContextAccessor _http;
        private readonly ILogger<UserService> _log;

        public UserService(IHttpContextAccessor http, ILogger<UserService> log)
        {
            _http = http;
            _log = log;
        }

        public AppUser GetCurrentUser()
        {
            // Prefer the authenticated HTTP identity; fall back to OS user
            var rawName = _http.HttpContext?.User?.Identity?.Name ?? Environment.UserName;

            // Strip domain prefix (DOMAIN\user or user@domain)
            var username = rawName.Contains('\\')
                ? rawName.Split('\\').Last()
                : rawName.Split('@').First();

            var displayName = username;

            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);
                using var user = UserPrincipal.FindByIdentity(ctx, username);
                if (user != null)
                    displayName = user.DisplayName ?? user.GivenName ?? username;
            }
            catch (Exception ex)
            {
                _log.LogWarning("Could not get display name from AD for {User}: {Msg}", username, ex.Message);
            }

            return new AppUser
            {
                Username = username,
                DisplayName = displayName,
                IsManager = AdminUsernames.Contains(username)
            };
        }
    }
}