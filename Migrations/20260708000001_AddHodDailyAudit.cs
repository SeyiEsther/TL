using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    public partial class AddHodDailyAudit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HodDailyAudits",
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
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuditType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalScore = table.Column<int>(type: "int", nullable: false),
                    MaxScore = table.Column<int>(type: "int", nullable: false),
                    EffectivenessJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionsRaised = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoodPractice = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditorSignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeamLeaderSignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HodDailyAudits", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "HodDailyAudits");
        }
    }
}
