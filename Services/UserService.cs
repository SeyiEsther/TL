using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using Microsoft.Extensions.Caching.Memory;
using TL.Models;

namespace TL.Services
{
    public class UserService
    {
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<UserService> _log;
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        private AppUser? _current;

        public UserService(IHttpContextAccessor http, ILogger<UserService> log, IMemoryCache cache)
        {
            _http = http;
            _log = log;
            _cache = cache;
        }

        public virtual AppUser GetCurrentUser()
        {
            if (_current != null) return _current;

            var identity = _http.HttpContext?.User?.Identity;
            if (identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(identity.Name))
            {
                var username = NormalizeAccountName(identity.Name);
                var displayName = _cache.GetOrCreate(
                    $"ad-display-name::{username}",
                    entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                        return ResolveDisplayName(username);
                    }) ?? username;

                _current = new AppUser
                {
                    Username = username,
                    DisplayName = displayName,
                    IsManager = true
                };
                return _current;
            }

            // IIS app pool / anonymous — not the person at the keyboard; don't pre-fill names.
            _current = new AppUser
            {
                Username = Environment.UserName ?? "unknown",
                DisplayName = "",
                IsManager = true
            };
            return _current;
        }

        public static string NormalizeAccountName(string identityName)
        {
            if (identityName.Contains('\\', StringComparison.Ordinal))
                return identityName.Split('\\').Last();
            if (identityName.Contains('@', StringComparison.Ordinal))
                return identityName.Split('@').First();
            return identityName;
        }

        private string ResolveDisplayName(string username)
        {
            if (!OperatingSystem.IsWindows())
                return username;

            return ResolveDisplayNameWindows(username);
        }

        [SupportedOSPlatform("windows")]
        private string ResolveDisplayNameWindows(string username)
        {
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);
                using var user = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, username);
                if (user != null)
                    return user.DisplayName ?? user.GivenName ?? username;
            }
            catch (Exception ex)
            {
                _log.LogWarning("Could not get display name from AD for {User}: {Msg}", username, ex.Message);
            }
            return username;
        }
    }
}
