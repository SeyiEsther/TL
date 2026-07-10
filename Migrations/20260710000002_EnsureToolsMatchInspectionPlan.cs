using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    /// <summary>
    /// Idempotent — safe when an earlier migration was skipped or the column is missing on deploy.
    /// </summary>
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
