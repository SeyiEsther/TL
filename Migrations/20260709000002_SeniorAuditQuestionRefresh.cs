using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    public partial class SeniorAuditQuestionRefresh : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "LeaderVisibilityCheck",
                table: "SeniorWeeklyAudits",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastTeamMeeting",
                table: "SeniorWeeklyAudits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "NcProcedureFollowed",
                table: "SeniorWeeklyAudits",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "StandardWorkMaintained",
                table: "SeniorWeeklyAudits",
                type: "tinyint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE SeniorWeeklyAudits
                SET NcProcedureFollowed = NcCaptureTrended
                WHERE NcProcedureFollowed IS NULL AND NcCaptureTrended IS NOT NULL;

                UPDATE SeniorWeeklyAudits
                SET StandardWorkMaintained = StandardWorkVisible
                WHERE StandardWorkMaintained IS NULL AND StandardWorkVisible IS NOT NULL;

                UPDATE SeniorWeeklyAudits
                SET LeaderVisibilityCheck = AbsenceManagedProactively
                WHERE LeaderVisibilityCheck IS NULL AND AbsenceManagedProactively IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LeaderVisibilityCheck", table: "SeniorWeeklyAudits");
            migrationBuilder.DropColumn(name: "LastTeamMeeting", table: "SeniorWeeklyAudits");
            migrationBuilder.DropColumn(name: "NcProcedureFollowed", table: "SeniorWeeklyAudits");
            migrationBuilder.DropColumn(name: "StandardWorkMaintained", table: "SeniorWeeklyAudits");
        }
    }
}
