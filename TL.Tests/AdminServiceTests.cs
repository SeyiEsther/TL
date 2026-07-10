using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TL.Models;
using TL.Services;

namespace TL.Tests;

public class AdminServiceTests
{
    [Theory]
    [InlineData("Oluwaseyifunmi Ogunbayo", true)]
    [InlineData("Mark McDonald", true)]
    [InlineData("George Thompson", true)]
    [InlineData("mark mcdonald", true)]
    [InlineData("Someone Else", false)]
    public void IsAdmin_matches_configured_display_names(string displayName, bool expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:DisplayNames:0"] = "Oluwaseyifunmi Ogunbayo",
                ["Admin:DisplayNames:1"] = "Mark McDonald",
                ["Admin:DisplayNames:2"] = "George Thompson",
            })
            .Build();

        var users = new StubUserService(displayName);
        var admin = new AdminService(config, users, NullLogger<AdminService>.Instance, new MemoryCache(new MemoryCacheOptions()));

        Assert.Equal(expected, admin.IsAdmin());
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
}
