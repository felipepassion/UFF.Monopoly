using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UFF.Monopoly.Migrations
{
    /// <inheritdoc />
    public partial class adding_pending_changes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000105"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BlockTemplates",
                columns: new[] { "Id", "BoardDefinitionId", "BuildingPricesCsv", "BuildingType", "Color", "CompanyId", "Description", "HotelPrice", "HousePrice", "ImageUrl", "Level", "LogoUrl", "Name", "Position", "Price", "Rent", "RentsCsv", "Slogan", "Type" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000105"), new Guid("11111111-1111-1111-1111-111111111111"), null, 0, "#d4af37", 0, "Lagoa (Muito Rica)", 1600, 800, "/images/blocks/property_basic.svg", 3, "", "Lagoa", 4, 8000, 80, "80,400,800,1440,1760,1920,2000", "", 1 });
        }
    }
}
