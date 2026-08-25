using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Tests;

public class MeetingHistoryTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public MeetingHistoryTests(FormSaveWebAppFactory factory) => _factory = factory;

    async Task SeedAsync(params (DateOnly date, string area, string shift)[] rows)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.TeamMeetings.RemoveRange(db.TeamMeetings);
        foreach (var (date, area, shift) in rows)
            db.TeamMeetings.Add(new TeamMeeting
            {
                MeetingDate = date, Area = area, Shift = shift,
                SubmittedBy = "tester", LastEditedBy = "Tester",
            });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Admin_can_delete_a_team_meeting()
    {
        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.TeamMeetings.RemoveRange(db.TeamMeetings);
            var m = new TeamMeeting { MeetingDate = new DateOnly(2026, 8, 4), Area = "Assembly 1", Shift = MeetingShifts.Day, SubmittedBy = "t" };
            db.TeamMeetings.Add(m);
            await db.SaveChangesAsync();
            id = m.Id;
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = System.Text.RegularExpressions.Regex.Match(
            await (await client.GetAsync("/MeetingHistory")).Content.ReadAsStringAsync(),
            @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        var resp = await client.PostAsync("/MeetingHistory?handler=Delete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["id"] = id.ToString(),
        }));
        Assert.Equal(System.Net.HttpStatusCode.Redirect, resp.StatusCode);

        using var check = _factory.Services.CreateScope();
        var db2 = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db2.TeamMeetings.AnyAsync(x => x.Id == id));
    }

    [Fact]
    public async Task Groups_meetings_under_their_rota_week()
    {
        // 03 Aug 2026 is the cycle-start Monday (Week A).
        await SeedAsync(
            (new DateOnly(2026, 8, 4), "Assembly 1", MeetingShifts.Day),
            (new DateOnly(2026, 8, 11), "Assembly 3", MeetingShifts.Night));

        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/MeetingHistory")).Content.ReadAsStringAsync();

        Assert.Contains("Assembly 1", html);
        Assert.Contains("Assembly 3", html);
        // Cycle labelling present for both A and B weeks.
        Assert.Contains("Cycle A", html);
        Assert.Contains("Cycle B", html);
    }

    [Fact]
    public async Task Empty_state_when_no_meetings()
    {
        await SeedAsync();
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/MeetingHistory")).Content.ReadAsStringAsync();
        Assert.Contains("No team meetings", html);
    }

    [Fact]
    public async Task Document_numbers_are_seeded_with_team_meeting_default()
    {
        using var scope = _factory.Services.CreateScope();
        var docs = scope.ServiceProvider.GetRequiredService<DocumentNumberService>();
        await docs.EnsureSeededAsync();
        Assert.Equal("CI038/01/1123/CW", docs.Get(DocumentFormTypes.TeamMeeting));
    }
}
