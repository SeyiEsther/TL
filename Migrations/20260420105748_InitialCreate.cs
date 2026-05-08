using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShiftSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TeamLeaderDisplay = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShiftDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HoursCompleted = table.Column<byte>(type: "tinyint", nullable: false),
                    Escalations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KeyRisks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priorities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutgoingTLSignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncomingTLSignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastEditedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastEditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_ShiftSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "ShiftSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HourlyChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftSubmissionId = table.Column<int>(type: "int", nullable: false),
                    HourNumber = table.Column<byte>(type: "tinyint", nullable: false),
                    HazardsObserved = table.Column<bool>(type: "bit", nullable: true),
                    UnsafeBehaviours = table.Column<bool>(type: "bit", nullable: true),
                    PositiveBehaviours = table.Column<bool>(type: "bit", nullable: true),
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
                    AccidentsReported = table.Column<bool>(type: "bit", nullable: true),
                    OverallSafetyStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverallQualityStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverallPerfStatus = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourlyChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HourlyChecks_ShiftSubmissions_ShiftSubmissionId",
                        column: x => x.ShiftSubmissionId,
                        principalTable: "ShiftSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SubmissionId",
                table: "AuditLogs",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyChecks_ShiftSubmissionId",
                table: "HourlyChecks",
                column: "ShiftSubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "HourlyChecks");

            migrationBuilder.DropTable(
                name: "ShiftSubmissions");
        }
    }
}
