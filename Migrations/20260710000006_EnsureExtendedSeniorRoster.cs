using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    public partial class EnsureExtendedSeniorRoster : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'PickerPersons', N'U') IS NOT NULL
                BEGIN
                    DECLARE @roster TABLE (SortOrder int, Name nvarchar(200));
                    INSERT INTO @roster (SortOrder, Name) VALUES
                    (1, 'Lukasz Jaworski'),
                    (2, 'Nicky Gleeson'),
                    (3, 'Vic Ward'),
                    (4, 'Simon Graham'),
                    (5, 'John Fisher'),
                    (6, 'Jonathan Maynard'),
                    (7, 'Dean Campbell'),
                    (8, 'Steven Hawkins'),
                    (9, 'Glen Atkinson'),
                    (10, 'Kyle Anderson'),
                    (11, 'Jim Gray'),
                    (12, 'Zoe Forest'),
                    (13, 'Patrick MacDonough'),
                    (14, 'Mark Tapp'),
                    (15, 'Tony Bent'),
                    (16, 'Tim Burda'),
                    (17, 'Intheiranath Subramaniam'),
                    (18, 'Andy Gill');

                    INSERT INTO PickerPersons (ListKind, Name, SortOrder)
                    SELECT 'Senior', r.Name, r.SortOrder
                    FROM @roster r
                    WHERE NOT EXISTS (
                        SELECT 1 FROM PickerPersons p
                        WHERE p.ListKind = 'Senior' AND p.Name = r.Name);

                    UPDATE p
                    SET p.SortOrder = r.SortOrder
                    FROM PickerPersons p
                    INNER JOIN @roster r ON p.ListKind = 'Senior' AND p.Name = r.Name;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
