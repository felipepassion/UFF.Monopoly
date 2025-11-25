using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UFF.Monopoly.Migrations
{
    /// <inheritdoc />
    public partial class adding_board_bg_image : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CenterImageUrl",
                table: "Boards",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000101"),
                columns: new[] { "Color", "Description", "HotelPrice", "HousePrice", "Level", "Name", "Price", "Rent", "RentsCsv", "Type" },
                values: new object[] { "", "Coleta salário ao passar.", 0, 0, null, "GO", 0, 0, null, 0 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"),
                columns: new[] { "Color", "Description", "HotelPrice", "HousePrice", "Level", "Name", "Price", "Rent", "RentsCsv", "Type" },
                values: new object[] { "", "Pague taxa fixa ao cair aqui.", 0, 0, null, "Imposto de Renda", 0, 200, null, 3 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000109"),
                columns: new[] { "Color", "Description", "HotelPrice", "HousePrice", "Level", "Name", "Price", "Rent", "RentsCsv", "Type" },
                values: new object[] { "", "Apenas visita.", 0, 0, null, "Cadeia / Visita", 0, 0, null, 4 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000118"),
                columns: new[] { "Color", "Description", "HotelPrice", "HousePrice", "Level", "Name", "Price", "Rent", "RentsCsv", "Type" },
                values: new object[] { "", "Siga diretamente para a prisão.", 0, 0, null, "Vá para a Prisão", 0, 1, null, 5 });

            migrationBuilder.UpdateData(
                table: "Boards",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CenterImageUrl",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CenterImageUrl",
                table: "Boards");

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000101"),
                columns: new[] { "Color", "Description", "HotelPrice", "HousePrice", "Level", "Name", "Price", "Rent", "RentsCsv", "Type" },
                values: new object[] { "#d4af37", "Leblon (Muito Rica)", 2000, 1000, 3, "Leblon", 10000, 100, "100,500,1000,1800,2200,2400,2500", 1 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"),
                columns: new[] { "Color", "Description", "HotelPrice", "HousePrice", "Level", "Name", "Price", "Rent", "RentsCsv", "Type" },
                values: new object[] { "#d4af37", "São Conrado (Muito Rica)", 1680, 840, 3, "São Conrado", 8400, 84, "84,420,840,1512,1848,2016,2100", 1 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000109"),
                columns: new[] { "Color", "Description", "HotelPrice", "HousePrice", "Level", "Name", "Price", "Rent", "RentsCsv", "Type" },
                values: new object[] { "#3498db", "Gávea (Rica)", 1100, 550, 2, "Gávea", 5500, 55, "55,275,550,990,1210,1320,1375", 1 });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000118"),
                columns: new[] { "Color", "Description", "HotelPrice", "HousePrice", "Level", "Name", "Price", "Rent", "RentsCsv", "Type" },
                values: new object[] { "#8b4513", "Campo Grande (Barata)", 180, 90, 0, "Campo Grande", 900, 9, "9,45,90,162,198,216,225", 1 });
        }
    }
}
