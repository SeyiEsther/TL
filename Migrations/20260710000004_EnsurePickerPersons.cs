using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL.Migrations
{
    public partial class EnsurePickerPersons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'PickerPersons', N'U') IS NULL
                BEGIN
                    CREATE TABLE PickerPersons (
                        Id int NOT NULL IDENTITY(1,1),
                        ListKind nvarchar(450) NOT NULL,
                        Name nvarchar(450) NOT NULL,
                        SortOrder int NOT NULL,
                        CONSTRAINT PK_PickerPersons PRIMARY KEY (Id)
                    );
                    CREATE UNIQUE INDEX IX_PickerPersons_ListKind_Name ON PickerPersons (ListKind, Name);
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM PickerPersons)
                BEGIN
                    INSERT INTO PickerPersons (ListKind, Name, SortOrder) VALUES
                    ('TeamLeader', 'Adam Wilczynski', 1),
                    ('TeamLeader', 'Cameron Thomson', 2),
                    ('TeamLeader', 'Carl Burnett', 3),
                    ('TeamLeader', 'Chris Anna', 4),
                    ('TeamLeader', 'Darran Bryce', 5),
                    ('TeamLeader', 'Deana Harvey', 6),
                    ('TeamLeader', 'Ian Davies', 7),
                    ('TeamLeader', 'Kamil Sowinski', 8),
                    ('TeamLeader', 'Leon Riglar', 9),
                    ('TeamLeader', 'Leon Sargent', 10),
                    ('TeamLeader', 'Leslie Grieve', 11),
                    ('TeamLeader', 'Marcin Oleksinski', 12),
                    ('TeamLeader', 'Marcin Rogaczewski', 13),
                    ('TeamLeader', 'Mariusz Tybusz', 14),
                    ('TeamLeader', 'Matthew Dundas', 15),
                    ('TeamLeader', 'Matthew Harding', 16),
                    ('TeamLeader', 'Michael Kirby', 17),
                    ('TeamLeader', 'Paul Worrall', 18),
                    ('TeamLeader', 'Phil Cook', 19),
                    ('TeamLeader', 'Steve Lomas', 20),
                    ('TeamLeader', 'Steven Morris', 21),
                    ('TeamLeader', 'Stuart Lancey', 22),
                    ('TeamLeader', 'Tomasz Bober', 23),
                    ('TeamLeader', 'Tomasz Dodacki', 24),
                    ('TeamLeader', 'Wojciech Duma', 25),
                    ('Hod', 'Damon Swain', 1),
                    ('Hod', 'John Smith', 2),
                    ('Hod', 'Kamil Sliwa', 3),
                    ('Hod', 'Ken Fenn', 4),
                    ('Hod', 'Michal Tymko', 5),
                    ('Hod', 'Paul Giles', 6),
                    ('Hod', 'Przemyslaw Zygnerski', 7),
                    ('Hod', 'Shaun Webber', 8),
                    ('Hod', 'Sion Llewellyn', 9),
                    ('Hod', 'Tyrone Marshall', 10);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS PickerPersons;");
        }
    }
}
