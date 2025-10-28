using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UFF.Monopoly.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupAndStateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HotelPrice",
                table: "BlockTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HousePrice",
                table: "BlockTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "BlockTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RentsCsv",
                table: "BlockTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "Blocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Hotels",
                table: "Blocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Houses",
                table: "Blocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HotelPrice",
                table: "BlockTemplates");

            migrationBuilder.DropColumn(
                name: "HousePrice",
                table: "BlockTemplates");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "BlockTemplates");

            migrationBuilder.DropColumn(
                name: "RentsCsv",
                table: "BlockTemplates");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "Hotels",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "Houses",
                table: "Blocks");
        }
    }
}
