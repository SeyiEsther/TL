using System.DirectoryServices.AccountManagement;
using TL.Models;

namespace TL.Services
{
    public class UserService
    {
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<UserService> _log;

        public UserService(IHttpContextAccessor http, ILogger<UserService> log)
        {
            _http = http;
            _log = log;
        }

        public AppUser GetCurrentUser()
        {
            var username = Environment.UserName;
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
                IsManager = true
            };
        }
    }
}