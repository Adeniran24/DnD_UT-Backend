using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVttInitiative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InitiativeActiveEntryId",
                table: "VttSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InitiativeRound",
                table: "VttSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "VttInitiativeEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    TokenId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VttInitiativeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VttInitiativeEntries_VttSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "VttSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VttInitiativeEntries_VttTokens_TokenId",
                        column: x => x.TokenId,
                        principalTable: "VttTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_VttInitiativeEntries_SessionId",
                table: "VttInitiativeEntries",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VttInitiativeEntries_TokenId",
                table: "VttInitiativeEntries",
                column: "TokenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VttInitiativeEntries");

            migrationBuilder.DropColumn(
                name: "InitiativeActiveEntryId",
                table: "VttSessions");

            migrationBuilder.DropColumn(
                name: "InitiativeRound",
                table: "VttSessions");
        }
    }
}
