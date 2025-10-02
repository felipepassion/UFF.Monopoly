using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UFF.Monopoly.Migrations
{
    /// <inheritdoc />
    public partial class fix_blocktemplate_fk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockTemplates_Boards_BoardDefinitionEntityId",
                table: "BlockTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_BlockTemplates_Boards_BoardDefinitionEntityId1",
                table: "BlockTemplates");

            migrationBuilder.DropIndex(
                name: "IX_BlockTemplates_BoardDefinitionEntityId",
                table: "BlockTemplates");

            migrationBuilder.DropIndex(
                name: "IX_BlockTemplates_BoardDefinitionEntityId1",
                table: "BlockTemplates");

            migrationBuilder.DropIndex(
                name: "IX_BlockTemplates_Position",
                table: "BlockTemplates");

            migrationBuilder.DropColumn(
                name: "BoardDefinitionEntityId",
                table: "BlockTemplates");

            migrationBuilder.DropColumn(
                name: "BoardDefinitionEntityId1",
                table: "BlockTemplates");

            migrationBuilder.AddColumn<Guid>(
                name: "BoardDefinitionId",
                table: "BlockTemplates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_BlockTemplates_BoardDefinitionId_Position",
                table: "BlockTemplates",
                columns: new[] { "BoardDefinitionId", "Position" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BlockTemplates_Boards_BoardDefinitionId",
                table: "BlockTemplates",
                column: "BoardDefinitionId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockTemplates_Boards_BoardDefinitionId",
                table: "BlockTemplates");

            migrationBuilder.DropIndex(
                name: "IX_BlockTemplates_BoardDefinitionId_Position",
                table: "BlockTemplates");

            migrationBuilder.DropColumn(
                name: "BoardDefinitionId",
                table: "BlockTemplates");

            migrationBuilder.AddColumn<Guid>(
                name: "BoardDefinitionEntityId",
                table: "BlockTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BoardDefinitionEntityId1",
                table: "BlockTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockTemplates_BoardDefinitionEntityId",
                table: "BlockTemplates",
                column: "BoardDefinitionEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockTemplates_BoardDefinitionEntityId1",
                table: "BlockTemplates",
                column: "BoardDefinitionEntityId1");

            migrationBuilder.CreateIndex(
                name: "IX_BlockTemplates_Position",
                table: "BlockTemplates",
                column: "Position",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BlockTemplates_Boards_BoardDefinitionEntityId",
                table: "BlockTemplates",
                column: "BoardDefinitionEntityId",
                principalTable: "Boards",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BlockTemplates_Boards_BoardDefinitionEntityId1",
                table: "BlockTemplates",
                column: "BoardDefinitionEntityId1",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
