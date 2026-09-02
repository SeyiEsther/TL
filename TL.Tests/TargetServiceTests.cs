using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Tests;

public class TargetServiceTests
{
    static (TargetService svc, AppDbContext db) Build()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"targets_{Guid.NewGuid():N}").Options);
        var svc = new TargetService(db, new MemoryCache(new MemoryCacheOptions()),
            NullLogger<TargetService>.Instance);
        return (svc, db);
    }

    [Fact]
    public async Task Seeds_defaults_then_reads_them_back()
    {
        var (svc, _) = Build();
        await svc.EnsureSeededAsync();
        Assert.Equal(35, svc.Shift);
        Assert.Equal(105, svc.Day);
        Assert.Equal(315, svc.Week);
    }

    [Fact]
    public async Task Reads_default_even_before_seeding()
    {
        var (svc, _) = Build();
        // No row yet — falls back to the built-in default, never throws.
        Assert.Equal(35, svc.Get(TargetKeys.Shift));
    }

    [Fact]
    public async Task Update_persists_value_and_records_who_and_when()
    {
        var (svc, db) = Build();
        await svc.EnsureSeededAsync();

        var ok = await svc.UpdateAsync(TargetKeys.Shift, 40, "George Thompson");
        Assert.True(ok);
        Assert.Equal(40, svc.Shift); // cache invalidated → new value read through

        var row = await db.TargetSettings.FirstAsync(t => t.Key == TargetKeys.Shift);
        Assert.Equal(40, row.Value);
        Assert.Equal("George Thompson", row.UpdatedBy);
        Assert.NotNull(row.UpdatedAt);
    }

    [Fact]
    public async Task Rejects_unknown_key_and_negative_value()
    {
        var (svc, _) = Build();
        Assert.False(await svc.UpdateAsync("NotAKey", 10, "admin"));
        Assert.False(await svc.UpdateAsync(TargetKeys.Day, -1, "admin"));
    }
}
