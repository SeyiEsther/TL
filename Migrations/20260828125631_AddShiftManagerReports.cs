using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftManagerReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShiftManagerReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ManagerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastEditedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastEditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManagerHseComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductionComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LswTeamLeaderComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LswHodComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aob = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftManagerReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftManagerReports_ReportDate_Shift",
                table: "ShiftManagerReports",
                columns: new[] { "ReportDate", "Shift" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftManagerReports");
        }
    }
}
