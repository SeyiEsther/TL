using TL.Models;

namespace TL.Services;

public static class HourMergeHelper
{
    public static bool IsHourComplete(HourInput inp) =>
        inp.Haz.HasValue && inp.Uns.HasValue && inp.Pos.HasValue
        && inp.Qchk.HasValue && inp.Dev.HasValue && inp.Nc.HasValue && inp.Qiplan.HasValue
        && inp.Tgt.HasValue && inp.Maint.HasValue && inp.Mat.HasValue && inp.Tools.HasValue
        && inp.Escl.HasValue && inp.Pconf.HasValue && inp.Pid.HasValue && inp.Ncp.HasValue
        && inp.Wb.HasValue && inp.Sup.HasValue && inp.Acc.HasValue
        && !string.IsNullOrWhiteSpace(inp.Ss)
        && !string.IsNullOrWhiteSpace(inp.Qs)
        && !string.IsNullOrWhiteSpace(inp.Ps);

    public static void MergeInto(HourlyCheck existing, HourInput inp, bool replaceAll)
    {
        if (replaceAll || inp.Haz.HasValue) existing.HazardsObserved = inp.Haz;
        if (replaceAll || inp.Uns.HasValue) existing.UnsafeBehaviours = inp.Uns;
        if (replaceAll || inp.Pos.HasValue) existing.PositiveBehaviours = inp.Pos;
        if (replaceAll || inp.Qchk.HasValue) existing.QualityChecksCompleted = inp.Qchk;
        if (replaceAll || inp.Dev.HasValue) existing.DeviationsEscalated = inp.Dev;
        if (replaceAll || inp.Nc.HasValue) existing.NonComplianceAddressed = inp.Nc;
        if (replaceAll || inp.Qiplan.HasValue) existing.ToolsMatchInspectionPlan = inp.Qiplan;
        if (replaceAll || inp.Tgt.HasValue) existing.HourlyTargetAchieved = inp.Tgt;
        if (replaceAll || inp.Maint.HasValue) existing.MaintenanceIssues = inp.Maint;
        if (replaceAll || inp.Mat.HasValue) existing.MaterialsAvailable = inp.Mat;
        if (replaceAll || inp.Tools.HasValue) existing.ToolsAvailable = inp.Tools;
        if (replaceAll || inp.Escl.HasValue) existing.EscalationsNeeded = inp.Escl;
        if (replaceAll || inp.Pconf.HasValue) existing.PartsConfirmed = inp.Pconf;
        if (replaceAll || inp.Pid.HasValue) existing.PartsIdCorrect = inp.Pid;
        if (replaceAll || inp.Ncp.HasValue) existing.NCPartsStoredCorrectly = inp.Ncp;
        if (replaceAll || inp.Wb.HasValue) existing.WellbeingConfirmed = inp.Wb;
        if (replaceAll || inp.Sup.HasValue) existing.SupportRequired = inp.Sup;
        if (replaceAll || inp.Acc.HasValue) existing.AccidentsReported = inp.Acc;
        if (replaceAll || inp.Sixs.HasValue) existing.SixSCompleted = inp.Sixs;
        if (replaceAll || inp.Tpm.HasValue) existing.TPMCompleted = inp.Tpm;

        if (replaceAll) existing.SafetyNotes = inp.Snote;
        else if (!string.IsNullOrWhiteSpace(inp.Snote)) existing.SafetyNotes = inp.Snote;

        if (replaceAll) existing.QualityNotes = inp.Qnote;
        else if (!string.IsNullOrWhiteSpace(inp.Qnote)) existing.QualityNotes = inp.Qnote;

        if (replaceAll) existing.PerformanceNotes = inp.Pnote;
        else if (!string.IsNullOrWhiteSpace(inp.Pnote)) existing.PerformanceNotes = inp.Pnote;

        if (replaceAll) existing.MoraleNotes = inp.Mnote;
        else if (!string.IsNullOrWhiteSpace(inp.Mnote)) existing.MoraleNotes = inp.Mnote;

        if (replaceAll) existing.OverallSafetyStatus = inp.Ss;
        else if (!string.IsNullOrWhiteSpace(inp.Ss)) existing.OverallSafetyStatus = inp.Ss;

        if (replaceAll) existing.OverallQualityStatus = inp.Qs;
        else if (!string.IsNullOrWhiteSpace(inp.Qs)) existing.OverallQualityStatus = inp.Qs;

        if (replaceAll) existing.OverallPerfStatus = inp.Ps;
        else if (!string.IsNullOrWhiteSpace(inp.Ps)) existing.OverallPerfStatus = inp.Ps;
    }
}
