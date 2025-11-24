using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UFF.Monopoly.Migrations
{
    /// <inheritdoc />
    public partial class updating_entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuildingPricesCsv",
                table: "BlockTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuildingType",
                table: "BlockTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BuildingLevel",
                table: "Blocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BuildingType",
                table: "Blocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000101"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000102"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000103"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000105"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000106"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000107"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000108"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000109"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000110"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000111"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000112"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000113"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000114"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000115"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000116"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000117"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000118"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000119"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000120"),
                columns: new[] { "BuildingPricesCsv", "BuildingType" },
                values: new object[] { null, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildingPricesCsv",
                table: "BlockTemplates");

            migrationBuilder.DropColumn(
                name: "BuildingType",
                table: "BlockTemplates");

            migrationBuilder.DropColumn(
                name: "BuildingLevel",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "BuildingType",
                table: "Blocks");
        }
    }
}
