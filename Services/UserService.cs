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

        // AD display-name lookups are expensive (a synchronous round-trip to the
        // domain controller). The layout resolves the current user on every page
        // render, and page models resolve it again, so without caching a single
        // request can hit AD multiple times. Cache the resolved name per username.
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        // Memoise within the (scoped) instance so repeated calls in the same
        // request never recompute, even for a cache miss.
        private AppUser? _current;

        public UserService(IHttpContextAccessor http, ILogger<UserService> log, IMemoryCache cache)
        {
            _http = http;
            _log = log;
            _cache = cache;
        }

        public AppUser GetCurrentUser()
        {
            if (_current != null) return _current;

            var username = Environment.UserName;
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
                using var user = UserPrincipal.FindByIdentity(ctx, username);
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
