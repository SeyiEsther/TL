using TL.Models;

namespace TL.Tests;

public class SeniorRotaTests
{
    static readonly string[] Eighteen = SeniorManagementList.Names;

    [Fact]
    public void TeamsForYear_always_has_four_people_per_group()
    {
        var teams = SeniorRota.TeamsForYear(2026, Eighteen);
        Assert.Equal(5, teams.Count);
        Assert.All(teams, t => Assert.Equal(4, t.Length));
    }

    [Fact]
    public void DutyGroupForWeek_always_has_four_distinct_people()
    {
        for (var week = 1; week <= 52; week++)
        {
            var group = SeniorRota.DutyGroupForWeek(2026, week, Eighteen);
            Assert.Equal(4, group.Length);
            Assert.Equal(4, group.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
    }

    [Fact]
    public void Duty_counts_are_balanced_across_the_year()
    {
        var counts = Eighteen.ToDictionary(n => n, _ => 0, StringComparer.OrdinalIgnoreCase);
        for (var week = 1; week <= 52; week++)
        {
            foreach (var person in SeniorRota.DutyGroupForWeek(2026, week, Eighteen))
                counts[person]++;
        }

        Assert.All(counts.Values, c => Assert.InRange(c, 11, 12));
    }

    [Fact]
    public void Week_six_uses_a_different_group_than_week_one()
    {
        var week1 = SeniorRota.DutyGroupForWeek(2026, 1, Eighteen);
        var week6 = SeniorRota.DutyGroupForWeek(2026, 6, Eighteen);
        Assert.False(week1.SequenceEqual(week6));
    }

    [Fact]
    public void Last_opening_rotation_team_is_padded_to_four()
    {
        var teams = SeniorRota.TeamsForYear(2026, Eighteen);
        var last = teams[^1];
        Assert.Equal(4, last.Length);
        Assert.Equal(4, last.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
