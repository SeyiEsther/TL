using TL.Models;
using TL.Services;

namespace TL.Tests;

public class HourMergeHelperTests
{
    [Fact]
    public void Partial_save_does_not_wipe_existing_hour_fields()
    {
        var existing = new HourlyCheck
        {
            HourNumber = 1,
            HazardsObserved = true,
            UnsafeBehaviours = false,
            PositiveBehaviours = true,
            QualityChecksCompleted = true,
            OverallSafetyStatus = "Green",
            OverallQualityStatus = "Amber",
            OverallPerfStatus = "Red",
        };

        var partial = new HourInput { Haz = true };

        HourMergeHelper.MergeInto(existing, partial, replaceAll: false);

        Assert.True(existing.HazardsObserved);
        Assert.False(existing.UnsafeBehaviours);
        Assert.True(existing.PositiveBehaviours);
        Assert.Equal("Green", existing.OverallSafetyStatus);
        Assert.Equal("Amber", existing.OverallQualityStatus);
        Assert.Equal("Red", existing.OverallPerfStatus);
    }

    [Fact]
    public void Complete_save_replaces_all_fields_including_cleared_answers()
    {
        var existing = new HourlyCheck
        {
            HourNumber = 1,
            HazardsObserved = true,
            OverallSafetyStatus = "Green",
        };

        var complete = BuildCompleteHourInput();
        complete.Haz = null;
        complete.Ss = null;

        HourMergeHelper.MergeInto(existing, complete, replaceAll: true);

        Assert.Null(existing.HazardsObserved);
        Assert.Null(existing.OverallSafetyStatus);
        Assert.False(existing.UnsafeBehaviours);
    }

    [Fact]
    public void IsHourComplete_matches_all_required_fields()
    {
        Assert.False(HourMergeHelper.IsHourComplete(new HourInput { Haz = true }));
        Assert.True(HourMergeHelper.IsHourComplete(BuildCompleteHourInput()));
    }

    static HourInput BuildCompleteHourInput() => new()
    {
        Haz = false, Uns = false, Pos = false,
        Qchk = true, Dev = false, Nc = false,
        Tgt = true, Maint = false, Mat = true, Tools = true,
        Escl = false, Pconf = true, Pid = true, Ncp = true,
        Wb = true, Sup = false, Acc = false,
        Ss = "Green", Qs = "Green", Ps = "Green",
    };
}
