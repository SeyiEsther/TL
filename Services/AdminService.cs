using Microsoft.Extensions.Caching.Memory;
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using TL.Models;

namespace TL.Services;

public class AdminService
{
    private readonly IConfiguration _config;
    private readonly UserService _users;
    private readonly ILogger<AdminService> _log;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan GroupCacheDuration = TimeSpan.FromMinutes(5);

    public AdminService(
        IConfiguration config, UserService users, ILogger<AdminService> log, IMemoryCache cache)
    {
        _config = config;
        _users = users;
        _log = log;
        _cache = cache;
    }

    public bool IsAdmin()
    {
        if (_config.GetValue("Admin:GrantAll", false))
            return true;

        var user = _users.GetCurrentUser();
        var usernames = _config.GetSection("Admin:Usernames").Get<string[]>() ?? [];
        if (usernames.Any(u => string.Equals(u.Trim(), user.Username, StringComparison.OrdinalIgnoreCase)))
            return true;

        var groups = _config.GetSection("Admin:AdGroups").Get<string[]>() ?? [];
        if (groups.Length == 0)
            return false;

        if (!OperatingSystem.IsWindows())
            return false;

        return IsMemberOfAnyGroupWindows(user.Username, groups);
    }

    [SupportedOSPlatform("windows")]
    bool IsMemberOfAnyGroupWindows(string username, string[] groups)
    {
        var cacheKey = $"admin-groups::{username}::{string.Join('|', groups)}";
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = GroupCacheDuration;
            try
            {
                using var ctx = new PrincipalContext(ContextType.Domain);
                using var user = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, username);
                if (user == null) return false;

                foreach (var groupName in groups)
                {
                    if (string.IsNullOrWhiteSpace(groupName)) continue;
                    using var group = GroupPrincipal.FindByIdentity(ctx, IdentityType.Name, groupName.Trim());
                    if (group != null && user.IsMemberOf(group))
                        return true;
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Could not check AD admin groups for {User}", username);
            }
            return false;
        });
    }
}
