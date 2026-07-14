using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    public partial class DropIncomingTlHandover : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('ShiftSubmissions', 'IncomingTLSignedAt') IS NOT NULL
                    ALTER TABLE ShiftSubmissions DROP COLUMN IncomingTLSignedAt;
                """);
            migrationBuilder.Sql("""
                IF COL_LENGTH('ShiftSubmissions', 'IncomingTLSignature') IS NOT NULL
                    ALTER TABLE ShiftSubmissions DROP COLUMN IncomingTLSignature;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('ShiftSubmissions', 'IncomingTLSignature') IS NULL
                    ALTER TABLE ShiftSubmissions ADD IncomingTLSignature nvarchar(max) NULL;
                """);
            migrationBuilder.Sql("""
                IF COL_LENGTH('ShiftSubmissions', 'IncomingTLSignedAt') IS NULL
                    ALTER TABLE ShiftSubmissions ADD IncomingTLSignedAt datetime2 NULL;
                """);
        }
    }
}
