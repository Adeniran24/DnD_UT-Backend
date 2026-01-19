using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApi.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileThemeJsonToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileThemeJson",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileThemeJson",
                table: "Users");
        }
    }
}
