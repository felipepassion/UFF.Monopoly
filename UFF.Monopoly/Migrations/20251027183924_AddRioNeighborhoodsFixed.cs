using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UFF.Monopoly.Migrations
{
    /// <inheritdoc />
    public partial class AddRioNeighborhoodsFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_ClientId",
                table: "UserProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "Rows",
                table: "Boards",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 5);

            migrationBuilder.AlterColumn<int>(
                name: "Cols",
                table: "Boards",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 5);

            migrationBuilder.AlterColumn<int>(
                name: "CellSizePx",
                table: "Boards",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 64);

            migrationBuilder.InsertData(
                table: "Boards",
                columns: new[] { "Id", "CellSizePx", "Cols", "CreatedAt", "Name", "Rows" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), 64, 5, new DateTime(2025, 10, 27, 18, 30, 24, 0, DateTimeKind.Utc), "Rio Sample Board", 5 });

            migrationBuilder.InsertData(
                table: "BlockTemplates",
                columns: new[] { "Id", "BoardDefinitionId", "Color", "Description", "HotelPrice", "HousePrice", "ImageUrl", "Level", "Name", "Position", "Price", "Rent", "RentsCsv", "Type" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000101"), new Guid("11111111-1111-1111-1111-111111111111"), "#d4af37", "Leblon (Muito Rica)", 2000, 1000, "/images/blocks/property_basic.svg", 3, "Leblon", 0, 10000, 100, "100,500,1000,1800,2200,2400,2500", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000102"), new Guid("11111111-1111-1111-1111-111111111111"), "#d4af37", "Ipanema (Muito Rica)", 1840, 920, "/images/blocks/property_basic.svg", 3, "Ipanema", 1, 9200, 92, "92,460,920,1656,2024,2208,2300", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000103"), new Guid("11111111-1111-1111-1111-111111111111"), "#d4af37", "Jardim Botânico (Muito Rica)", 1760, 880, "/images/blocks/property_basic.svg", 3, "Jardim Botânico", 2, 8800, 88, "88,440,880,1584,1936,2112,2200", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000104"), new Guid("11111111-1111-1111-1111-111111111111"), "#d4af37", "São Conrado (Muito Rica)", 1680, 840, "/images/blocks/property_basic.svg", 3, "São Conrado", 3, 8400, 84, "84,420,840,1512,1848,2016,2100", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000105"), new Guid("11111111-1111-1111-1111-111111111111"), "#d4af37", "Lagoa (Muito Rica)", 1600, 800, "/images/blocks/property_basic.svg", 3, "Lagoa", 4, 8000, 80, "80,400,800,1440,1760,1920,2000", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000106"), new Guid("11111111-1111-1111-1111-111111111111"), "#3498db", "Copacabana (Rica)", 1400, 700, "/images/blocks/property_basic.svg", 2, "Copacabana", 5, 7000, 70, "70,350,700,1260,1540,1680,1750", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000107"), new Guid("11111111-1111-1111-1111-111111111111"), "#3498db", "Flamengo (Rica)", 1300, 650, "/images/blocks/property_basic.svg", 2, "Flamengo", 6, 6500, 65, "65,325,650,1170,1430,1560,1625", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000108"), new Guid("11111111-1111-1111-1111-111111111111"), "#3498db", "Botafogo (Rica)", 1200, 600, "/images/blocks/property_basic.svg", 2, "Botafogo", 7, 6000, 60, "60,300,600,1080,1320,1440,1500", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000109"), new Guid("11111111-1111-1111-1111-111111111111"), "#3498db", "Gávea (Rica)", 1100, 550, "/images/blocks/property_basic.svg", 2, "Gávea", 8, 5500, 55, "55,275,550,990,1210,1320,1375", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000110"), new Guid("11111111-1111-1111-1111-111111111111"), "#3498db", "Laranjeiras (Rica)", 1000, 500, "/images/blocks/property_basic.svg", 2, "Laranjeiras", 9, 5000, 50, "50,250,500,900,1100,1200,1250", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000111"), new Guid("11111111-1111-1111-1111-111111111111"), "#27ae60", "Humaitá (Mediana)", 700, 350, "/images/blocks/property_basic.svg", 1, "Humaitá", 10, 3500, 35, "35,175,350,630,770,840,875", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000112"), new Guid("11111111-1111-1111-1111-111111111111"), "#27ae60", "Leme (Mediana)", 640, 320, "/images/blocks/property_basic.svg", 1, "Leme", 11, 3200, 32, "32,160,320,576,704,768,800", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000113"), new Guid("11111111-1111-1111-1111-111111111111"), "#27ae60", "Maracanã (Mediana)", 600, 300, "/images/blocks/property_basic.svg", 1, "Maracanã", 12, 3000, 30, "30,150,300,540,660,720,750", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000114"), new Guid("11111111-1111-1111-1111-111111111111"), "#27ae60", "Tijuca (Mediana)", 560, 280, "/images/blocks/property_basic.svg", 1, "Tijuca", 13, 2800, 28, "28,140,280,504,616,672,700", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000115"), new Guid("11111111-1111-1111-1111-111111111111"), "#27ae60", "Andaraí (Mediana)", 500, 250, "/images/blocks/property_basic.svg", 1, "Andaraí", 14, 2500, 25, "25,125,250,450,550,600,625", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000116"), new Guid("11111111-1111-1111-1111-111111111111"), "#8b4513", "Madureira (Barata)", 240, 120, "/images/blocks/property_basic.svg", 0, "Madureira", 15, 1200, 12, "12,60,120,216,264,288,300", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000117"), new Guid("11111111-1111-1111-1111-111111111111"), "#8b4513", "Bonsucesso (Barata)", 200, 100, "/images/blocks/property_basic.svg", 0, "Bonsucesso", 16, 1000, 10, "10,50,100,180,220,240,250", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000118"), new Guid("11111111-1111-1111-1111-111111111111"), "#8b4513", "Campo Grande (Barata)", 180, 90, "/images/blocks/property_basic.svg", 0, "Campo Grande", 17, 900, 9, "9,45,90,162,198,216,225", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000119"), new Guid("11111111-1111-1111-1111-111111111111"), "#8b4513", "Realengo (Barata)", 160, 80, "/images/blocks/property_basic.svg", 0, "Realengo", 18, 800, 8, "8,40,80,144,176,192,200", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000120"), new Guid("11111111-1111-1111-1111-111111111111"), "#8b4513", "Paciência (Barata)", 120, 60, "/images/blocks/property_basic.svg", 0, "Paciência", 19, 600, 6, "6,30,60,108,132,144,150", 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000101"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000102"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000103"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000105"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000106"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000107"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000108"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000109"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000110"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000111"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000112"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000113"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000114"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000115"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000116"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000117"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000118"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000119"));

            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000120"));

            migrationBuilder.DeleteData(
                table: "Boards",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.AlterColumn<int>(
                name: "Rows",
                table: "Boards",
                type: "integer",
                nullable: false,
                defaultValue: 5,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Cols",
                table: "Boards",
                type: "integer",
                nullable: false,
                defaultValue: 5,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "CellSizePx",
                table: "Boards",
                type: "integer",
                nullable: false,
                defaultValue: 64,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_ClientId",
                table: "UserProfiles",
                column: "ClientId",
                unique: true);
        }
    }
}
