using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    public partial class SeniorAuditThreePointScoring : Migration
    {
        static readonly string[] ScoreColumns =
        [
            "HandoverStandardsFollowed", "VisualManagementCurrent", "EscalationPathsUsed",
            "PpeComplianceFull", "NearMissesReported", "SafetyActionLogCurrent",
            "FirstOffRecordsComplete", "NcCaptureTrended", "QualityGatesMaintained",
            "AbsenceManagedProactively", "TlsCoachingTeams", "TrainingMatrixCurrent",
            "SixSStandardMaintained", "TpmScheduleFollowed", "StandardWorkVisible",
            "TrackingAgainstWeeklyPlan", "MetricsVisibleAndOwned", "ImprovementActionsProgressing",
        ];

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var col in ScoreColumns)
            {
                var tmp = $"{col}_Tmp";
                migrationBuilder.AddColumn<byte>(name: tmp, table: "SeniorWeeklyAudits", type: "tinyint", nullable: true);
                migrationBuilder.Sql($"""
                    UPDATE SeniorWeeklyAudits
                    SET {tmp} = CASE
                        WHEN {col} = 1 THEN 2
                        WHEN {col} = 0 THEN 0
                        ELSE NULL
                    END
                    """);
                migrationBuilder.DropColumn(name: col, table: "SeniorWeeklyAudits");
                migrationBuilder.RenameColumn(name: tmp, table: "SeniorWeeklyAudits", newName: col);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var col in ScoreColumns)
            {
                var tmp = $"{col}_Tmp";
                migrationBuilder.AddColumn<bool>(name: tmp, table: "SeniorWeeklyAudits", type: "bit", nullable: true);
                migrationBuilder.Sql($"""
                    UPDATE SeniorWeeklyAudits
                    SET {tmp} = CASE
                        WHEN {col} = 2 THEN 1
                        WHEN {col} = 0 THEN 0
                        ELSE NULL
                    END
                    """);
                migrationBuilder.DropColumn(name: col, table: "SeniorWeeklyAudits");
                migrationBuilder.RenameColumn(name: tmp, table: "SeniorWeeklyAudits", newName: col);
            }
        }
    }
}
