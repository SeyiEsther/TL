using TL.Models;
using TL.Services;

namespace TL.Tests;

public class PdfExportSmokeTests
{
    [Fact]
    public void GenerateHodDaily_Tpm_SheetMetal_does_not_throw()
    {
        var dept = "Phase 3 Pierce and Fold";
        var questions = HodAuditDefinitions.GetQuestions(HodAuditTypes.Tpm, dept);
        Assert.True(questions.Count > 20, $"expected many TPM questions, got {questions.Count}");

        var answers = questions.Select((q, i) => new HodAuditAnswer
        {
            QuestionId = q.Id,
            Section = q.Section,
            Label = q.Label,
            MachineName = q.MachineName,
            Pass = i % 3 != 0,
            Evidence = i % 2 == 0 ? "Checked board and signed sheets were current." : null,
        }).ToList();

        var audit = new HodDailyAudit
        {
            AuditorName = "Test Auditor",
            AuditDate = DateOnly.FromDateTime(DateTime.Today),
            Department = dept,
            EffectivenessArea = AreaList.GetLabelsForDepartment(dept)[0],
            Area = AreaList.GetLabelsForDepartment(dept)[0],
            AuditType = HodAuditTypes.Tpm,
            AnswersJson = HodAuditSerializer.ToJson(answers),
            TotalScore = answers.Count(a => a.Pass == true),
            MaxScore = answers.Count,
            ActionsRaised = "Follow up on stale boards in Zone 2",
            GoodPractice = "Zone 1 boards exemplary",
            AuditorSignature = "Test Auditor",
            TeamLeaderSignature = "TL Name",
        };

        var pdf = new PdfExportService();
        var bytes = pdf.GenerateHodDaily(audit, answers, null);
        Assert.True(bytes.Length > 1000);
        // PDF magic
        Assert.Equal(0x25, bytes[0]); // %
        Assert.Equal(0x50, bytes[1]); // P
    }

    [Fact]
    public void GenerateSeniorWeekly_does_not_throw()
    {
        var a = new SeniorWeeklyAudit
        {
            AuditorName = "Senior",
            AuditDate = DateOnly.FromDateTime(DateTime.Today),
            Area = "Zone 1",
            HandoverStandardsFollowed = 2,
            VisualManagementCurrent = 1,
            EscalationPathsUsed = 2,
            PpeComplianceFull = 2,
            NearMissesReported = 1,
            FirstOffRecordsComplete = 2,
            NcProcedureFollowed = 2,
            QualityGatesMaintained = 1,
            LeaderVisibilityCheck = 2,
            TrainingMatrixCurrent = 2,
            SixSStandardMaintained = 1,
            TpmScheduleFollowed = 2,
            StandardWorkMaintained = 2,
            TrackingAgainstWeeklyPlan = 1,
            ImprovementActionsProgressing = 2,
            GoodPracticeObserved = "Good",
            AreasForImprovement = "Improve",
            ActionsRaised = "Action",
            OverallVerdict = "Green",
            AuditorSignature = "Senior",
        };
        var bytes = new PdfExportService().GenerateSeniorWeekly(a);
        Assert.True(bytes.Length > 500);
        Assert.Equal(0x25, bytes[0]);
    }

    [Fact]
    public void GenerateShift_does_not_throw()
    {
        var s = new ShiftSubmission
        {
            TeamLeaderDisplay = "TL",
            ShiftDate = DateOnly.FromDateTime(DateTime.Today),
            Shift = "Day",
            Area = "Zone 1",
            HoursCompleted = 2,
            Escalations = "None",
            KeyRisks = "Risk",
            Priorities = "Priority",
            OutgoingTLSignature = "TL",
            Hours =
            [
                new HourlyCheck { HourNumber = 1, HazardsObserved = false, HourlyTargetAchieved = true, OverallSafetyStatus = "Green", OverallQualityStatus = "Green", OverallPerfStatus = "Green" },
                new HourlyCheck { HourNumber = 2, HazardsObserved = true, HourlyTargetAchieved = false, OverallSafetyStatus = "Amber", OverallQualityStatus = "Green", OverallPerfStatus = "Amber" },
            ],
        };
        var bytes = new PdfExportService().GenerateShift(s);
        Assert.True(bytes.Length > 500);
        Assert.Equal(0x25, bytes[0]);
    }
}
