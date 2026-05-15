using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    public partial class AddMissedTargetReasons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MissedTargetReasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReasonText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissedTargetReasons", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "MissedTargetReasonId",
                table: "HourlyChecks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissedTargetNote",
                table: "HourlyChecks",
                type: "nvarchar(max)",
                nullable: true);

            // Seed default reasons
            migrationBuilder.InsertData(
                table: "MissedTargetReasons",
                columns: new[] { "ReasonText", "SortOrder", "IsActive" },
                values: new object[,]
                {
                    { "Waiting for parts", 10, true },
                    { "Machine breakdown", 20, true },
                    { "Resource / staffing shortage", 30, true },
                    { "Quality rework required", 40, true },
                    { "Material shortage", 50, true },
                    { "Training / new operator", 60, true },
                    { "Safety stop / incident", 70, true },
                    { "Other", 100, true }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MissedTargetReasonId", table: "HourlyChecks");
            migrationBuilder.DropColumn(name: "MissedTargetNote", table: "HourlyChecks");
            migrationBuilder.DropTable(name: "MissedTargetReasons");
        }
    }
}
