using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMeetings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamMeetings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeetingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Supervisor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostCentre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Team = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompiledOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MeetingDateTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Actions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HealthSafety = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerSatisfaction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kpis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployeeSatisfaction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aob = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MeetingResults = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinutesKeeper = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProblemsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeamMembersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupMembersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuestsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastEditedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastEditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMeetings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamMeetings_MeetingDate_Area_Shift",
                table: "TeamMeetings",
                columns: new[] { "MeetingDate", "Area", "Shift" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamMeetings");
        }
    }
}
