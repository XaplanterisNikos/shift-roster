using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vardiologio.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the supervisors who sign the individual monthly sheet (new table "Signatories") and
    /// each employee's supervisor (new nullable column "Employees.SignatoryId").
    /// Additive only: a CREATE TABLE and an ALTER TABLE … ADD COLUMN. No existing row or column
    /// is changed, and the Employees table is not rebuilt (the column has no foreign key).
    /// </summary>
    public partial class AddSignatories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every existing employee starts without a supervisor (NULL) until one is chosen.
            migrationBuilder.AddColumn<int>(
                name: "SignatoryId",
                table: "Employees",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Signatories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Role = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signatories", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Signatories");

            migrationBuilder.DropColumn(
                name: "SignatoryId",
                table: "Employees");
        }
    }
}
