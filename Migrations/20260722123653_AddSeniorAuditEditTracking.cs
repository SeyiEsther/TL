using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    /// <inheritdoc />
    public partial class AddSeniorAuditEditTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastEditedAt",
                table: "SeniorWeeklyAudits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastEditedBy",
                table: "SeniorWeeklyAudits",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastEditedAt",
                table: "SeniorWeeklyAudits");

            migrationBuilder.DropColumn(
                name: "LastEditedBy",
                table: "SeniorWeeklyAudits");
        }
    }
}
