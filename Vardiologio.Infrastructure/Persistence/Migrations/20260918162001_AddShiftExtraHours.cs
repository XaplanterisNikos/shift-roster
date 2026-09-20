using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vardiologio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftExtraHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShiftExtraHours",
                columns: table => new
                {
                    ShiftCodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftExtraHours", x => x.ShiftCodeId);
                    table.ForeignKey(
                        name: "FK_ShiftExtraHours_ShiftCodes_ShiftCodeId",
                        column: x => x.ShiftCodeId,
                        principalTable: "ShiftCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftExtraHours");
        }
    }
}
