using Microsoft.EntityFrameworkCore.Migrations;
using TL.Models;

#nullable disable

namespace TL.Migrations
{
    public partial class AddPickerPersons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PickerPersons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ListKind = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickerPersons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickerPersons_ListKind_Name",
                table: "PickerPersons",
                columns: new[] { "ListKind", "Name" },
                unique: true);

            var sort = 1;
            foreach (var name in new[]
            {
                "Adam Wilczynski", "Cameron Thomson", "Carl Burnett", "Chris Anna",
                "Darran Bryce", "Deana Harvey", "Ian Davies", "Kamil Sowinski",
                "Leon Riglar", "Leon Sargent", "Leslie Grieve", "Marcin Oleksinski",
                "Marcin Rogaczewski", "Mariusz Tybusz", "Matthew Dundas", "Matthew Harding",
                "Michael Kirby", "Paul Worrall", "Phil Cook", "Steve Lomas",
                "Steven Morris", "Stuart Lancey", "Tomasz Bober", "Tomasz Dodacki",
                "Wojciech Duma",
            })
            {
                migrationBuilder.InsertData(
                    table: "PickerPersons",
                    columns: new[] { "ListKind", "Name", "SortOrder" },
                    values: new object[] { PersonListKinds.TeamLeader, name, sort++ });
            }

            sort = 1;
            foreach (var name in new[]
            {
                "Damon Swain", "John Smith", "Kamil Sliwa", "Ken Fenn",
                "Michal Tymko", "Paul Giles", "Przemyslaw Zygnerski", "Shaun Webber",
                "Sion Llewellyn", "Tyrone Marshall",
            })
            {
                migrationBuilder.InsertData(
                    table: "PickerPersons",
                    columns: new[] { "ListKind", "Name", "SortOrder" },
                    values: new object[] { PersonListKinds.Hod, name, sort++ });
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PickerPersons");
        }
    }
}
