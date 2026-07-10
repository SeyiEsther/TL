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
        Assert.False(access.CanAccessPage("/History"));
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
    public void Senior_list_member_sees_senior_and_management_not_hod()
    {
        var access = CreateAccess("Jim Gray", grantAll: false);
        Assert.False(access.CanAccessHod());
        Assert.True(access.CanAccessSenior());
        Assert.True(access.CanAccessManagement());
        Assert.False(access.CanAccessPage("/AuditStart"));
        Assert.True(access.CanAccessPage("/SeniorRota"));
    }

    [Fact]
    public void Shift_manager_sees_senior_and_management_like_group1()
    {
        var access = CreateAccess("Mike Tregillis", grantAll: false);
        Assert.False(access.CanAccessHod());
        Assert.True(access.CanAccessSenior());
        Assert.True(access.CanAccessManagement());
        Assert.True(access.CanAccessPage("/SeniorStart"));
        Assert.True(access.CanAccessPage("/SeniorDashboard"));
        Assert.True(access.CanAccessPage("/History"));
        Assert.False(access.CanAccessPage("/AuditStart"));
    }

    [Theory]
    [InlineData("Piotr Pelka")]
    [InlineData("alison gilley")]
    public void All_shift_managers_match_case_insensitively(string displayName)
    {
        var access = CreateAccess(displayName, grantAll: false);
        Assert.True(access.CanAccessSenior());
        Assert.True(access.CanAccessPage("/Dashboard"));
    }

    static PortalAccessService CreateAccess(string displayName, bool grantAll)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:GrantAll"] = grantAll ? "true" : "false",
            })
            .Build();

        var users = new StubUserService(displayName);
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

        public StubUserService(string displayName)
            : base(new HttpContextAccessor(), NullLogger<UserService>.Instance, new MemoryCache(new MemoryCacheOptions()))
        {
            _user = new AppUser
            {
                Username = "testuser",
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
