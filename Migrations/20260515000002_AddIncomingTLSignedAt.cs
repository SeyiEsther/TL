using Microsoft.EntityFrameworkCore.Migrations;
namespace TL.Migrations
{
    public partial class AddIncomingTLSignedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IncomingTLSignedAt",
                table: "ShiftSubmissions",
                type: "datetime2",
                nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IncomingTLSignedAt", table: "ShiftSubmissions");
        }
    }
}
