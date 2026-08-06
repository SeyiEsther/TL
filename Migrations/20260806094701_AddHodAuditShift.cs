using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    /// <inheritdoc />
    public partial class AddHodAuditShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The Shift column already exists on the live database (added out of
            // band). Guard the add so startup Migrate() is a no-op there, while a
            // fresh deploy still gets the column. Keeps EF history consistent
            // either way.
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.HodDailyAudits') AND name = 'Shift')
BEGIN
    ALTER TABLE [dbo].[HodDailyAudits] ADD [Shift] nvarchar(40) NULL;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.HodDailyAudits') AND name = 'Shift')
BEGIN
    ALTER TABLE [dbo].[HodDailyAudits] DROP COLUMN [Shift];
END");
        }
    }
}
