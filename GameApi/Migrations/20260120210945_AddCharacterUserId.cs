using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "userId",
                table: "characters",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_characters_userId",
                table: "characters",
                column: "userId");

            migrationBuilder.AddForeignKey(
                name: "FK_characters_Users_userId",
                table: "characters",
                column: "userId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_characters_Users_userId",
                table: "characters");

            migrationBuilder.DropIndex(
                name: "IX_characters_userId",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "userId",
                table: "characters");
        }
    }
}
