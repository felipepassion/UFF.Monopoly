using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UFF.Monopoly.Migrations
{
    /// <inheritdoc />
    public partial class add_board_layout_fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CellSizePx",
                table: "Boards",
                type: "integer",
                nullable: false,
                defaultValue: 64);

            migrationBuilder.AddColumn<int>(
                name: "Cols",
                table: "Boards",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "Rows",
                table: "Boards",
                type: "integer",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CellSizePx",
                table: "Boards");

            migrationBuilder.DropColumn(
                name: "Cols",
                table: "Boards");

            migrationBuilder.DropColumn(
                name: "Rows",
                table: "Boards");
        }
    }
}
