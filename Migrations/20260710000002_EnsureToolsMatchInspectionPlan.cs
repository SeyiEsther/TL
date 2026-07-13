using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    public partial class EnsureToolsMatchInspectionPlan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('HourlyChecks', 'ToolsMatchInspectionPlan') IS NULL
                    ALTER TABLE HourlyChecks ADD ToolsMatchInspectionPlan bit NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('HourlyChecks', 'ToolsMatchInspectionPlan') IS NOT NULL
                    ALTER TABLE HourlyChecks DROP COLUMN ToolsMatchInspectionPlan;
                """);
        }
    }
}
