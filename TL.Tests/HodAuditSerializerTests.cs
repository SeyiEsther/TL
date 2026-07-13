using TL.Services;

namespace TL.Tests;

public class HodAuditSerializerTests
{
    [Fact]
    public void ParseEffectiveness_null_issues_and_linked_failures_become_empty_lists()
    {
        var json = """
            [{"teamLeader":"Adam","issues":null,"linkedAuditFailures":null}]
            """;

        var findings = HodAuditSerializer.ParseEffectiveness(json);

        Assert.Single(findings);
        Assert.NotNull(findings[0].Issues);
        Assert.NotNull(findings[0].LinkedAuditFailures);
        Assert.Empty(findings[0].Issues);
        Assert.Empty(findings[0].LinkedAuditFailures);
    }

    [Fact]
    public void ParseEffectiveness_invalid_json_returns_empty_list()
    {
        Assert.Empty(HodAuditSerializer.ParseEffectiveness("{not json"));
    }
}

public class PortalNameMatcherTests
{
    [Fact]
    public void Matches_single_token_name_without_throwing()
    {
        Assert.False(PortalNameMatcher.Matches("Ken Fenn", "Fenn"));
    }

    [Fact]
    public void Matches_ken_to_kenneth_fenn()
    {
        Assert.True(PortalNameMatcher.Matches("Ken Fenn", "Kenneth Fenn"));
        Assert.True(PortalNameMatcher.Matches("Ken Fenn", "ken"));
    }
}
