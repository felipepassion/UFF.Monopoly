using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UFF.Monopoly.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockStateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HotelPrice",
                table: "Blocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HousePrice",
                table: "Blocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "Blocks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RentsCsv",
                table: "Blocks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HotelPrice",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "HousePrice",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "RentsCsv",
                table: "Blocks");
        }
    }
}
