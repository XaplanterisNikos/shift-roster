using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vardiologio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportColors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Hex = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    PreviousHex = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportColors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportColors_Key",
                table: "ReportColors",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportColors");
        }
    }
}
