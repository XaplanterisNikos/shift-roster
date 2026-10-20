using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vardiologio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHoursRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllowedDays",
                table: "ShiftCodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HourLimits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ToCompleteMonthly = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SimpleMonthly = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    NightMonthly = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SundayMonthly = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    HolidayPerHoliday = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ToCompletePlusHolidayMonthly = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourLimits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftCodeHours",
                columns: table => new
                {
                    ShiftCodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DayType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftCodeHours", x => new { x.ShiftCodeId, x.DayType, x.Category });
                    table.ForeignKey(
                        name: "FK_ShiftCodeHours_ShiftCodes_ShiftCodeId",
                        column: x => x.ShiftCodeId,
                        principalTable: "ShiftCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Date",
                table: "Holidays",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "HourLimits");

            migrationBuilder.DropTable(
                name: "ShiftCodeHours");

            migrationBuilder.DropColumn(
                name: "AllowedDays",
                table: "ShiftCodes");
        }
    }
}
