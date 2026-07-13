using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    public partial class RenameKenFennToKennethFenn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'PickerPersons', N'U') IS NOT NULL
                BEGIN
                    UPDATE PickerPersons
                    SET Name = N'Kenneth Fenn'
                    WHERE ListKind = N'Hod' AND Name = N'Ken Fenn';

                    IF NOT EXISTS (
                        SELECT 1 FROM PickerPersons
                        WHERE ListKind = N'Hod' AND Name = N'Kenneth Fenn')
                    AND NOT EXISTS (
                        SELECT 1 FROM PickerPersons
                        WHERE ListKind = N'Hod' AND Name = N'Ken Fenn')
                    BEGIN
                        INSERT INTO PickerPersons (ListKind, Name, SortOrder)
                        VALUES (N'Hod', N'Kenneth Fenn', 4);
                    END
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'PickerPersons', N'U') IS NOT NULL
                BEGIN
                    UPDATE PickerPersons
                    SET Name = N'Ken Fenn'
                    WHERE ListKind = N'Hod' AND Name = N'Kenneth Fenn';
                END
                """);
        }
    }
}
