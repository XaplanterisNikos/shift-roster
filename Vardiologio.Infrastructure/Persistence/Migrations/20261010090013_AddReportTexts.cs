using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vardiologio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportTexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Form = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Section = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Block = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Line = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    PreviousText = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTexts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportTexts_Form_Block_Line",
                table: "ReportTexts",
                columns: new[] { "Form", "Block", "Line" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportTexts");
        }
    }
}
