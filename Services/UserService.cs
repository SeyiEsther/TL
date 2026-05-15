using System.DirectoryServices.AccountManagement;
using TL.Data;
using TL.Models;

namespace TL.Services
{
    public class UserService
    {
        private readonly IHttpContextAccessor _http;
        private readonly AppDbContext _db;
        private readonly ILogger<UserService> _log;

        public UserService(IHttpContextAccessor http, AppDbContext db, ILogger<UserService> log)
        {
            _http = http;
            _db = db;
            _log = log;
        }

        public AppUser GetCurrentUser()
        {
            var rawName = _http.HttpContext?.User?.Identity?.Name ?? Environment.UserName;

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

            var isManager = _db.AdminUsers.Any(u => u.Username == username);

            return new AppUser
            {
                Username = username,
                DisplayName = displayName,
                IsManager = isManager
            };
        }
    }
}
