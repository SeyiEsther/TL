using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Tests;

public class SeniorRosterSyncTests
{
    [Fact]
    public async Task Sync_adds_missing_senior_names_to_database()
    {
        var dbName = $"SeniorSync_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var db = new AppDbContext(options);
        db.PickerPersons.AddRange(
            new PickerPerson { ListKind = PersonListKinds.Senior, Name = "Jim Gray", SortOrder = 1 },
            new PickerPerson { ListKind = PersonListKinds.Senior, Name = "Mark Tapp", SortOrder = 2 });
        await db.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new PersonListService(db, cache, NullLogger<PersonListService>.Instance);
        await service.EnsureLoadedAsync();

        var seniors = await db.PickerPersons
            .Where(p => p.ListKind == PersonListKinds.Senior)
            .Select(p => p.Name)
            .ToListAsync();

        Assert.Equal(SeniorManagementList.Names.Length, seniors.Count);
        Assert.Contains("Nicky Gleeson", seniors);
        Assert.Contains("Zoe Forest", seniors);
        Assert.Contains("Andy Gill", seniors);
        Assert.Equal(SeniorManagementList.Names.Length, service.Seniors.Count);
    }
}
