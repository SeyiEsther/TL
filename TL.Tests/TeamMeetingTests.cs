using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using TL.Data;
using TL.Models;

namespace TL.Tests;

public class TeamMeetingTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public TeamMeetingTests(FormSaveWebAppFactory factory) => _factory = factory;

    async Task ResetAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.TeamMeetings.RemoveRange(db.TeamMeetings);
        await db.SaveChangesAsync();
    }

    static async Task<int> RecordAsync(HttpClient client, string date, string area, string shift, string actions, int? editingId = null)
    {
        var getUrl = $"/MeetingSession?date={date}&area={Uri.EscapeDataString(area)}&shift={shift}";
        var token = ExtractToken(await (await client.GetAsync(getUrl)).Content.ReadAsStringAsync())!;
        var body = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("meetingDate", date),
            new("area", area),
            new("shift", shift),
            new("Supervisor", "Sam Super"),
            new("Actions", actions),
            new("ProblemsJson", "[{\"problem\":\"Line stop\",\"action\":\"Fix jig\",\"responsible\":\"Bob\",\"status\":2}]"),
            new("TeamMembersJson", "[{\"name\":\"Alice\",\"present\":true}]"),
            new("GroupMembersJson", "[]"),
            new("GuestsJson", "[{\"name\":\"Visitor\",\"department\":\"QA\",\"telephone\":\"123\"}]"),
        };
        if (editingId.HasValue) body.Add(new("editingId", editingId.Value.ToString()));

        var resp = await client.PostAsync("/MeetingSession", new FormUrlEncodedContent(body));
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var loc = resp.Headers.Location?.ToString() ?? "";
        Assert.Contains("/Success", loc);
        var m = Regex.Match(loc, @"meetingId=(\d+)");
        Assert.True(m.Success, $"No meetingId in redirect: {loc}");
        return int.Parse(m.Groups[1].Value);
    }

    [Fact]
    public async Task Reopening_same_slot_updates_the_shared_record()
    {
        await ResetAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var date = DateTime.Today.ToString("yyyy-MM-dd");

        var id1 = await RecordAsync(client, date, "Assembly 1", MeetingShifts.Day, "First pass");
        var id2 = await RecordAsync(client, date, "Assembly 1", MeetingShifts.Day, "Second pass"); // no editingId

        Assert.Equal(id1, id2); // same shared record

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.TeamMeetings.CountAsync());
        var row = await db.TeamMeetings.SingleAsync();
        Assert.Equal("Second pass", row.Actions);   // updated
        Assert.NotNull(row.LastEditedAt);            // tracking recorded
    }

    [Fact]
    public async Task Different_shift_same_day_area_is_a_separate_record()
    {
        await ResetAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var date = DateTime.Today.ToString("yyyy-MM-dd");

        await RecordAsync(client, date, "Assembly 1", MeetingShifts.Day, "day");
        await RecordAsync(client, date, "Assembly 1", MeetingShifts.Night, "night");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.TeamMeetings.CountAsync());
    }

    [Fact]
    public async Task Paint_slot_records_and_continuity_works_the_same_as_other_areas()
    {
        await ResetAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var date = DateTime.Today.ToString("yyyy-MM-dd");

        // Paint shows "Flexible" instead of a time, but the slot must still open the
        // form and save (Area = "Paint", a normal shift name).
        var id1 = await RecordAsync(client, date, "Paint", MeetingShifts.Day, "Paint first");
        // Reopening the same slot (date + Paint + shift) continues the SAME record.
        var id2 = await RecordAsync(client, date, "Paint", MeetingShifts.Day, "Paint second");

        Assert.Equal(id1, id2);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.TeamMeetings.CountAsync(m => m.Area == "Paint"));
        var row = await db.TeamMeetings.SingleAsync(m => m.Area == "Paint");
        Assert.Equal("Paint second", row.Actions);
    }

    [Fact]
    public async Task New_meeting_page_shows_the_adhoc_start_form()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/MeetingSession")).Content.ReadAsStringAsync();
        // No slot chosen → the ad-hoc "New team meeting" starter, not a dead end.
        Assert.Contains("New team meeting", html);
        Assert.Contains("name=\"area\"", html);
        Assert.Contains("name=\"shift\"", html);
    }

    [Fact]
    public async Task Pdf_generates_after_save()
    {
        await ResetAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        var id = await RecordAsync(client, date, "Assembly 2", MeetingShifts.Back, "content");

        var resp = await client.GetAsync($"/api/meetings/{id}/pdf");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("application/pdf", resp.Content.Headers.ContentType?.MediaType);
        Assert.True((await resp.Content.ReadAsByteArrayAsync()).Length > 500);
    }

    static string? ExtractToken(string html)
    {
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        if (!m.Success)
            m = Regex.Match(html, @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""");
        return m.Success ? m.Groups[1].Value : null;
    }
}
