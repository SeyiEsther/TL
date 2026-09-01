using Microsoft.AspNetCore.Mvc.Testing;

namespace TL.Tests;

public class ShiftManagerAuditContextTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public ShiftManagerAuditContextTests(FormSaveWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Audit_started_from_shift_manager_is_branded_and_returns_there()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/AuditStart?from=sm")).Content.ReadAsStringAsync();
        Assert.Contains("Shift Manager audit", html);        // branded, not HOD
        Assert.Contains("Back to Shift Manager", html);       // returns to the SM tab
        Assert.Contains("value=\"sm\"", html);               // context carried in the form
    }

    [Fact]
    public async Task Audit_started_normally_keeps_hod_branding()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/AuditStart")).Content.ReadAsStringAsync();
        Assert.Contains("Head of Department", html);
        Assert.DoesNotContain("Back to Shift Manager", html);
    }
}
