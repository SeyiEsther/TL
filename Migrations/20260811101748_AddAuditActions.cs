using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    SourceLabel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AuditDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RaisedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RaisedByUsername = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RaisedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OwnerKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OwnerIsExternal = table.Column<bool>(type: "bit", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CompletionNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditActions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditActions_SourceType_SourceId",
                table: "AuditActions",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditActions_Status_OwnerKey",
                table: "AuditActions",
                columns: new[] { "Status", "OwnerKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditActions");
        }
    }
}
