using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vardiologio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductionLookupAndShiftSegment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Segment",
                table: "ShiftCodes",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmploymentTypeId",
                table: "Employees",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkPositionId",
                table: "Employees",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmploymentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPositions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmploymentTypeId",
                table: "Employees",
                column: "EmploymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_WorkPositionId",
                table: "Employees",
                column: "WorkPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentTypes_Code",
                table: "EmploymentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkPositions_Name",
                table: "WorkPositions",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_EmploymentTypes_EmploymentTypeId",
                table: "Employees",
                column: "EmploymentTypeId",
                principalTable: "EmploymentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_WorkPositions_WorkPositionId",
                table: "Employees",
                column: "WorkPositionId",
                principalTable: "WorkPositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_EmploymentTypes_EmploymentTypeId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_WorkPositions_WorkPositionId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "EmploymentTypes");

            migrationBuilder.DropTable(
                name: "WorkPositions");

            migrationBuilder.DropIndex(
                name: "IX_Employees_EmploymentTypeId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_WorkPositionId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Segment",
                table: "ShiftCodes");

            migrationBuilder.DropColumn(
                name: "EmploymentTypeId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WorkPositionId",
                table: "Employees");
        }
    }
}
