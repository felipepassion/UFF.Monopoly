using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UFF.Monopoly.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyFieldsToBlockTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "BlockTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "BlockTemplates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slogan",
                table: "BlockTemplates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000101"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000102"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000103"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000105"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000106"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000107"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000108"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000109"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000110"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000111"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000112"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000113"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000114"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000115"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000116"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000117"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000118"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000119"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });

            migrationBuilder.UpdateData(
                table: "BlockTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000120"),
                columns: new[] { "CompanyId", "LogoUrl", "Slogan" },
                values: new object[] { 0, "", "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "BlockTemplates");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "BlockTemplates");

            migrationBuilder.DropColumn(
                name: "Slogan",
                table: "BlockTemplates");
        }
    }
}
