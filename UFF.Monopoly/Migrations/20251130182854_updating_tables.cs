using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UFF.Monopoly.Migrations
{
    /// <inheritdoc />
    public partial class updating_tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000109"),
                columns: new[] { "Description", "Name", "Type" },
                values: new object[] { "Livre.", "Estacionamento", 7 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000115"),
                columns: new[] { "HotelPrice", "HousePrice" },
                values: new object[] { 700, 500 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000118"),
                column: "Type",
                value: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000109"),
                columns: new[] { "Description", "Name", "Type" },
                values: new object[] { "Apenas visita.", "Cadeia / Visita", 4 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000115"),
                columns: new[] { "HotelPrice", "HousePrice" },
                values: new object[] { 500, 250 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000118"),
                column: "Type",
                value: 5);
        }
    }
}
