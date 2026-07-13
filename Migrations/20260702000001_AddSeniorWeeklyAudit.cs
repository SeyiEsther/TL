using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    public partial class AddSeniorWeeklyAudit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeniorWeeklyAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuditorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuditDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HandoverStandardsFollowed = table.Column<bool>(type: "bit", nullable: true),
                    VisualManagementCurrent = table.Column<bool>(type: "bit", nullable: true),
                    EscalationPathsUsed = table.Column<bool>(type: "bit", nullable: true),
                    GovernanceNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PpeComplianceFull = table.Column<bool>(type: "bit", nullable: true),
                    NearMissesReported = table.Column<bool>(type: "bit", nullable: true),
                    SafetyActionLogCurrent = table.Column<bool>(type: "bit", nullable: true),
                    SafetyNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstOffRecordsComplete = table.Column<bool>(type: "bit", nullable: true),
                    NcCaptureTrended = table.Column<bool>(type: "bit", nullable: true),
                    QualityGatesMaintained = table.Column<bool>(type: "bit", nullable: true),
                    QualityNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AbsenceManagedProactively = table.Column<bool>(type: "bit", nullable: true),
                    TlsCoachingTeams = table.Column<bool>(type: "bit", nullable: true),
                    TrainingMatrixCurrent = table.Column<bool>(type: "bit", nullable: true),
                    PeopleNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SixSStandardMaintained = table.Column<bool>(type: "bit", nullable: true),
                    TpmScheduleFollowed = table.Column<bool>(type: "bit", nullable: true),
                    StandardWorkVisible = table.Column<bool>(type: "bit", nullable: true),
                    StandardsNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrackingAgainstWeeklyPlan = table.Column<bool>(type: "bit", nullable: true),
                    MetricsVisibleAndOwned = table.Column<bool>(type: "bit", nullable: true),
                    ImprovementActionsProgressing = table.Column<bool>(type: "bit", nullable: true),
                    PerformanceNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoodPracticeObserved = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AreasForImprovement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionsRaised = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverallVerdict = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditorSignature = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeniorWeeklyAudits", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SeniorWeeklyAudits");
        }
    }
}
