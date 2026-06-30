using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    public partial class AddAuditSubmissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastEditedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastEditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AuditorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuditDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TLOnShift = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShiftObserved = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HazardsObserved = table.Column<bool>(type: "bit", nullable: true),
                    UnsafeBehavioursObserved = table.Column<bool>(type: "bit", nullable: true),
                    PositiveBehavioursPraised = table.Column<bool>(type: "bit", nullable: true),
                    SafetyNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QualityChecksCompleted = table.Column<bool>(type: "bit", nullable: true),
                    DeviationsEscalated = table.Column<bool>(type: "bit", nullable: true),
                    NonComplianceAddressed = table.Column<bool>(type: "bit", nullable: true),
                    QualityNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HourlyTargetAchieved = table.Column<bool>(type: "bit", nullable: true),
                    MaintenanceIssues = table.Column<bool>(type: "bit", nullable: true),
                    MaterialsAvailable = table.Column<bool>(type: "bit", nullable: true),
                    ToolsAvailable = table.Column<bool>(type: "bit", nullable: true),
                    EscalationsNeeded = table.Column<bool>(type: "bit", nullable: true),
                    PartsConfirmed = table.Column<bool>(type: "bit", nullable: true),
                    PartsIdCorrect = table.Column<bool>(type: "bit", nullable: true),
                    NCPartsStoredCorrectly = table.Column<bool>(type: "bit", nullable: true),
                    SixSCompleted = table.Column<bool>(type: "bit", nullable: true),
                    TPMCompleted = table.Column<bool>(type: "bit", nullable: true),
                    PerformanceNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WellbeingConfirmed = table.Column<bool>(type: "bit", nullable: true),
                    SupportRequired = table.Column<bool>(type: "bit", nullable: true),
                    MoraleNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccidentsObserved = table.Column<bool>(type: "bit", nullable: true),
                    OverallSafetyStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverallQualityStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverallPerfStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionsRaised = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoodPracticeObserved = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FollowUpRequired = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionOwner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AuditorSignature = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditSubmissions", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditSubmissions");
        }
    }
}
