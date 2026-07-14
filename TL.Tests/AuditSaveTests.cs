using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Tests;

public class AuditSaveTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;

    public AuditSaveTests(FormSaveWebAppFactory factory) => _factory = factory;

    async Task ResetDbAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.HodDailyAudits.RemoveRange(db.HodDailyAudits);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task AuditStart_creates_draft_and_Audit_shows_Save_changes()
    {
        await ResetDbAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var department = "Sheet Metal";
        var area = AreaList.GetLabelsForDepartment(department)[0];

        var startHtml = await (await client.GetAsync("/AuditStart")).Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(startHtml)!;

        var startResp = await client.PostAsync("/AuditStart", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["auditDate"] = today,
            ["auditorName"] = "Test Auditor",
            ["department"] = department,
            ["effectivenessArea"] = area,
            ["auditType"] = HodAuditTypes.Tpm,
        }));
        Assert.Equal(HttpStatusCode.Redirect, startResp.StatusCode);
        var location = startResp.Headers.Location?.ToString() ?? "";
        Assert.Contains("id=", location);
        var id = ParseIdFromLocation(location);
        Assert.True(id > 0);

        var auditHtml = await (await client.GetAsync($"/Audit?id={id}")).Content.ReadAsStringAsync();
        Assert.Contains("Save changes", auditHtml);
        Assert.Contains("formnovalidate", auditHtml);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var draft = await db.HodDailyAudits.SingleAsync(a => a.Id == id);
        Assert.Equal(HodAuditTypes.Tpm, draft.AuditType);
        Assert.Equal(department, draft.Department);
        Assert.True(string.IsNullOrWhiteSpace(draft.AuditorSignature));
    }

    [Fact]
    public async Task SaveProgress_persists_answers_without_signature()
    {
        await ResetDbAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var department = "Assembly";
        var area = AreaList.GetLabelsForDepartment(department)[0];

        var id = await CreateDraftAsync(client, today, department, area, HodAuditTypes.SixS);
        var formHtml = await (await client.GetAsync($"/Audit?id={id}")).Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(formHtml)!;
        var questions = HodAuditDefinitions.GetQuestions(HodAuditTypes.SixS, department);

        var body = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("auditDate", today),
            new("auditorName", "Test Auditor"),
            new("department", department),
            new("effectivenessArea", area),
            new("auditType", HodAuditTypes.SixS),
            new("editingId", id.ToString()),
            new("Actions", "Follow up on TPM board"),
            new("GoodPractice", "Clean area"),
        };
        for (var i = 0; i < questions.Count; i++)
        {
            body.Add(new($"A[{i}].QuestionId", questions[i].Id));
            body.Add(new($"A[{i}].Pass", "True"));
            body.Add(new($"A[{i}].Evidence", $"note-{i}"));
        }

        var saveResp = await client.PostAsync($"/Audit?handler=SaveProgress&id={id}", new FormUrlEncodedContent(body));
        Assert.Equal(HttpStatusCode.Redirect, saveResp.StatusCode);
        Assert.Contains($"id={id}", saveResp.Headers.Location?.ToString() ?? "");
        Assert.Contains("saved=progress", saveResp.Headers.Location?.ToString() ?? "");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.HodDailyAudits.SingleAsync(a => a.Id == id);
        Assert.Equal("Follow up on TPM board", saved.ActionsRaised);
        Assert.Equal("Clean area", saved.GoodPractice);
        var answers = HodAuditSerializer.ParseAnswers(saved.AnswersJson);
        Assert.Equal(questions.Count, answers.Count);
        Assert.All(answers, a => Assert.True(a.Pass));
        Assert.True(string.IsNullOrWhiteSpace(saved.AuditorSignature));
    }

    [Fact]
    public async Task SaveProgress_keeps_answers_on_page_when_id_missing()
    {
        await ResetDbAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var department = "Assembly";
        var area = AreaList.GetLabelsForDepartment(department)[0];
        var questions = HodAuditDefinitions.GetQuestions(HodAuditTypes.SixS, department);

        var startHtml = await (await client.GetAsync("/AuditStart")).Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(startHtml)!;

        var body = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("auditDate", today),
            new("auditorName", "Test Auditor"),
            new("department", department),
            new("effectivenessArea", area),
            new("auditType", HodAuditTypes.SixS),
        };
        for (var i = 0; i < questions.Count; i++)
        {
            body.Add(new($"A[{i}].QuestionId", questions[i].Id));
            body.Add(new($"A[{i}].Pass", "True"));
        }

        var resp = await client.PostAsync("/Audit?handler=SaveProgress", new FormUrlEncodedContent(body));
        var html = await resp.Content.ReadAsStringAsync();
        Assert.True(resp.IsSuccessStatusCode, $"status={(int)resp.StatusCode} body={html[..Math.Min(500, html.Length)]}");
        Assert.Contains("Could not save", html, StringComparison.OrdinalIgnoreCase);
    }

    static async Task<int> CreateDraftAsync(
        HttpClient client, string today, string department, string area, string type)
    {
        var startHtml = await (await client.GetAsync("/AuditStart")).Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(startHtml)!;
        var startResp = await client.PostAsync("/AuditStart", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["auditDate"] = today,
            ["auditorName"] = "Test Auditor",
            ["department"] = department,
            ["effectivenessArea"] = area,
            ["auditType"] = type,
        }));
        return ParseIdFromLocation(startResp.Headers.Location?.ToString());
    }

    static int ParseIdFromLocation(string? location)
    {
        var m = Regex.Match(location ?? "", @"[?&]id=(\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    static string? ExtractAntiforgeryToken(string html)
    {
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        if (!m.Success)
            m = Regex.Match(html, @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""");
        return m.Success ? m.Groups[1].Value : null;
    }
}
