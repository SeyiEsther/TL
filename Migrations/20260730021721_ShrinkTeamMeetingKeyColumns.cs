using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    /// <inheritdoc />
    public partial class ShrinkTeamMeetingKeyColumns : Migration
    {
        // Area/Shift are part of IX_TeamMeetings_MeetingDate_Area_Shift, and
        // SQL Server won't ALTER a column an index depends on — so drop the
        // index, resize, then recreate it. New key size: date(3) + Area(400) +
        // Shift(60) = 463 bytes, well under the 1700-byte limit.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeamMeetings_MeetingDate_Area_Shift",
                table: "TeamMeetings");

            migrationBuilder.AlterColumn<string>(
                name: "Shift",
                table: "TeamMeetings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Area",
                table: "TeamMeetings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMeetings_MeetingDate_Area_Shift",
                table: "TeamMeetings",
                columns: new[] { "MeetingDate", "Area", "Shift" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeamMeetings_MeetingDate_Area_Shift",
                table: "TeamMeetings");

            migrationBuilder.AlterColumn<string>(
                name: "Shift",
                table: "TeamMeetings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Area",
                table: "TeamMeetings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMeetings_MeetingDate_Area_Shift",
                table: "TeamMeetings",
                columns: new[] { "MeetingDate", "Area", "Shift" });
        }
    }
}
