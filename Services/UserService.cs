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

        // Does a name typed by an admin resolve to a real AD user?
        //   true  = an AD user has this exact display name
        //   false = no AD user matches (likely a typo / nickname mismatch)
        //   null  = can't tell (not on Windows, or AD lookup failed)
        public bool? DisplayNameResolvesToAd(string? name)
        {
            var n = (name ?? "").Trim();
            if (n.Length == 0) return false;
            if (!OperatingSystem.IsWindows()) return null;

            return _cache.GetOrCreate($"ad-resolve::{n.ToLowerInvariant()}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return ResolveExistsWindows(n);
            });
        }

        [SupportedOSPlatform("windows")]
        private bool? ResolveExistsWindows(string name)
        {
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);
                // Match on display name first (how AD shows the person).
                using (var byDisplay = new PrincipalSearcher(new UserPrincipal(ctx) { DisplayName = name }))
                    if (byDisplay.FindOne() != null) return true;
                // Fall back to "First Last" → given name + surname.
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    using var byNames = new PrincipalSearcher(new UserPrincipal(ctx)
                    {
                        GivenName = parts[0],
                        Surname = parts[^1],
                    });
                    if (byNames.FindOne() != null) return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "AD display-name resolve failed for {Name}", name);
                return null; // unknown — don't alarm the admin on an AD hiccup
            }
        }
    }
}
