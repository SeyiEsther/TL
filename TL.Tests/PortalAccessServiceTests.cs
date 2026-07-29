using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TL.Models;
using TL.Services;

namespace TL.Tests;

public class PortalAccessServiceTests
{
    [Fact]
    public void General_team_leader_only_sees_home_pages()
    {
        var access = CreateAccess("Adam Wilczynski", grantAll: false);
        Assert.False(access.CanAccessHod());
        Assert.False(access.CanAccessSenior());
        Assert.False(access.CanAccessManagement());
        Assert.True(access.CanAccessPage("/Index"));
        Assert.True(access.CanAccessPage("/Form"));
        Assert.True(access.CanAccessPage("/History")); // history is viewable by everyone
        Assert.True(access.CanAccessPage("/MeetingRota")); // reference page, no role restriction
        Assert.False(access.CanAccessPage("/ShiftHistory"));
        Assert.False(access.CanAccessPage("/HodHistory"));
        Assert.False(access.CanAccessPage("/Today"));
        Assert.False(access.CanAccessPage("/Dashboard"));
    }

    [Fact]
    public void Hod_list_member_sees_hod_and_management_not_senior()
    {
        var access = CreateAccess("Damon Swain", grantAll: false);
        Assert.True(access.CanAccessHod());
        Assert.False(access.CanAccessSenior());
        Assert.True(access.CanAccessManagement());
        Assert.True(access.CanAccessPage("/HodDashboard"));
        Assert.False(access.CanAccessPage("/SeniorStart"));
    }

    [Fact]
    public void Senior_on_full_access_list_sees_hod_and_senior()
    {
        // Jim Gray is on both SeniorManagementList and FullAccess (ShiftManagerList),
        // so FullAccess grants HoD as well as Senior.
        var access = CreateAccess("Jim Gray", grantAll: false);
        Assert.True(access.CanAccessHod());
        Assert.True(access.CanAccessSenior());
        Assert.True(access.CanAccessManagement());
        Assert.True(access.CanAccessPage("/AuditStart"));
        Assert.True(access.CanAccessPage("/SeniorRota"));
        Assert.False(access.CanAccessPage("/Admin"));
    }

    [Fact]
    public void General_team_leader_cannot_open_today_overview()
    {
        var access = CreateAccess("Adam Wilczynski", grantAll: false);
        Assert.False(access.CanAccessPage("/Today"));
        Assert.False(access.CanAccessApi("/api/submissions/today"));
        Assert.False(access.CanAccessApi("/api/audits/hod/1/pdf"));
    }

    [Fact]
    public void Shift_manager_sees_hod_senior_home_and_today()
    {
        var access = CreateAccess("Mike Tregillis", grantAll: false);
        Assert.True(access.CanAccessHod());
        Assert.True(access.CanAccessSenior());
        Assert.True(access.CanAccessManagement());
        Assert.True(access.CanAccessPage("/Index"));
        Assert.True(access.CanAccessPage("/Today"));
        Assert.True(access.CanAccessPage("/HodDashboard"));
        Assert.True(access.CanAccessPage("/AuditStart"));
        Assert.True(access.CanAccessPage("/SeniorStart"));
        Assert.True(access.CanAccessPage("/SeniorDashboard"));
        Assert.True(access.CanAccessPage("/ShiftHistory"));
        Assert.True(access.CanAccessPage("/HodHistory"));
        Assert.True(access.CanAccessPage("/SeniorHistory"));
    }

    [Theory]
    [InlineData("Piotr Pelka")]
    [InlineData("alison gilley")]
    public void All_shift_managers_match_case_insensitively(string displayName)
    {
        var access = CreateAccess(displayName, grantAll: false);
        Assert.True(access.CanAccessHod());
        Assert.True(access.CanAccessSenior());
        Assert.True(access.CanAccessPage("/Dashboard"));
        Assert.True(access.CanAccessPage("/Today"));
    }

    [Theory]
    [InlineData("Kenneth Fenn", "Kenneth Fenn")]
    [InlineData("ken", "Kenneth Fenn")]
    public void Hod_kenneth_fenn_matches_display_name_and_username(string username, string displayName)
    {
        var access = CreateAccess(displayName, username, grantAll: false);
        Assert.True(access.CanAccessHod());
        Assert.True(access.CanAccessPage("/HodDashboard"));
    }

    [Fact]
    public void PortalNameMatcher_treats_ken_and_kenneth_as_same_person_with_surname()
    {
        Assert.True(PortalNameMatcher.Matches("Ken Fenn", "Kenneth Fenn"));
        Assert.True(PortalNameMatcher.Matches("Kenneth Fenn", "Ken Fenn"));
        Assert.True(PortalNameMatcher.Matches("Ken Fenn", "ken"));
    }

    static PortalAccessService CreateAccess(string displayName, bool grantAll) =>
        CreateAccess(displayName, "testuser", grantAll);

    static PortalAccessService CreateAccess(string displayName, string username, bool grantAll)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:GrantAll"] = grantAll ? "true" : "false",
            })
            .Build();

        var users = new StubUserService(displayName, username);
        var admin = new AdminService(config, users, NullLogger<AdminService>.Instance, new MemoryCache(new MemoryCacheOptions()));
        var people = new PersonListService(
            new InMemoryPortalDb(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<PersonListService>.Instance);
        people.ReloadAsync().GetAwaiter().GetResult();
        return new PortalAccessService(admin, users, people);
    }

    sealed class StubUserService : UserService
    {
        readonly AppUser _user;

        public StubUserService(string displayName, string username = "testuser")
            : base(new HttpContextAccessor(), NullLogger<UserService>.Instance, new MemoryCache(new MemoryCacheOptions()))
        {
            _user = new AppUser
            {
                Username = username,
                DisplayName = displayName,
                IsManager = true,
            };
        }

        public override AppUser GetCurrentUser() => _user;
    }

    sealed class InMemoryPortalDb : Data.AppDbContext
    {
        public InMemoryPortalDb()
            : base(new DbContextOptionsBuilder<Data.AppDbContext>()
                .UseInMemoryDatabase($"PortalAccessTests_{Guid.NewGuid():N}")
                .Options)
        {
            Database.EnsureCreated();
        }
    }
}
