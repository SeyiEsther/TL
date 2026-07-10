using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    /// <summary>Backfills senior management names and any missing default picker names.</summary>
    public partial class EnsureSeniorAndMissingPickerNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'PickerPersons', N'U') IS NOT NULL
                BEGIN
                    DECLARE @names TABLE (ListKind nvarchar(50), Name nvarchar(200), SortOrder int);
                    INSERT INTO @names (ListKind, Name, SortOrder) VALUES
                    ('Senior', 'Jim Gray', 1),
                    ('Senior', 'John Fisher', 2),
                    ('Senior', 'Steven Hawkins', 3),
                    ('Senior', 'Vic Ward', 4),
                    ('Senior', 'Simon Graham', 5),
                    ('Senior', 'Lukasz Jaworski', 6),
                    ('Senior', 'Dean Campbell', 7),
                    ('Senior', 'Glen Atkinson', 8),
                    ('Senior', 'Kyle Anderson', 9),
                    ('Senior', 'Jonathan Maynard', 10),
                    ('Senior', 'Mark Tapp', 11),
                    ('Senior', 'Tony Bent', 12);

                    INSERT INTO PickerPersons (ListKind, Name, SortOrder)
                    SELECT n.ListKind, n.Name, n.SortOrder
                    FROM @names n
                    WHERE NOT EXISTS (
                        SELECT 1 FROM PickerPersons p
                        WHERE p.ListKind = n.ListKind AND p.Name = n.Name);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
