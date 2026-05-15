using System.DirectoryServices.AccountManagement;
using TL.Models;

namespace TL.Services
{
    public class UserService
    {
        // Checked against both short username and full email UPN
        private static readonly HashSet<string> AdminIdentities = new(StringComparer.OrdinalIgnoreCase)
        {
            "kgwynjones", "kgwynjones@rittal-csm.co.uk",
            "ljaworski",  "ljaworski@rittal-csm.co.uk",
            "oogunbayo",  "oogunbayo@rittal-csm.co.uk",
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
            var rawName = _http.HttpContext?.User?.Identity?.Name ?? Environment.UserName;

            // Strip domain prefix — handles DOMAIN\user and user@domain
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

            // Match on raw identity (email/UPN), stripped username, or DOMAIN\user suffix
            var isManager = AdminIdentities.Contains(rawName) || AdminIdentities.Contains(username);

            return new AppUser
            {
                Username = username,
                DisplayName = displayName,
                IsManager = isManager
            };
        }
    }
}
