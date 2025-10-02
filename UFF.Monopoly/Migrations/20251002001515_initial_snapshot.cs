using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UFF.Monopoly.Migrations
{
    /// <inheritdoc />
    public partial class initial_snapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Boards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentPlayerIndex = table.Column<int>(type: "integer", nullable: false),
                    IsFinished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlockTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    Rent = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    BoardDefinitionEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    BoardDefinitionEntityId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockTemplates_Boards_BoardDefinitionEntityId",
                        column: x => x.BoardDefinitionEntityId,
                        principalTable: "Boards",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BlockTemplates_Boards_BoardDefinitionEntityId1",
                        column: x => x.BoardDefinitionEntityId1,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    Rent = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsMortgaged = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    GameStateId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blocks_Games_GameStateId",
                        column: x => x.GameStateId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CurrentPosition = table.Column<int>(type: "integer", nullable: false),
                    Money = table.Column<int>(type: "integer", nullable: false),
                    InJail = table.Column<bool>(type: "boolean", nullable: false),
                    GetOutOfJailFreeCards = table.Column<int>(type: "integer", nullable: false),
                    JailTurns = table.Column<int>(type: "integer", nullable: false),
                    IsBankrupt = table.Column<bool>(type: "boolean", nullable: false),
                    GameStateId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Games_GameStateId",
                        column: x => x.GameStateId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_GameStateId",
                table: "Blocks",
                column: "GameStateId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Players_GameStateId",
                table: "Players",
                column: "GameStateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "BlockTemplates");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Boards");

            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}
