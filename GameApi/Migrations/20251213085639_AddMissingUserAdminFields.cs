using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingUserAdminFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
{
    // ROLE
    migrationBuilder.AddColumn<string>(
        name: "Role",
        table: "Users",
        type: "longtext",
        nullable: false,
        defaultValue: "User");

    // IS ACTIVE
    migrationBuilder.AddColumn<bool>(
        name: "IsActive",
        table: "Users",
        nullable: false,
        defaultValue: true);

    // LAST LOGIN
    migrationBuilder.AddColumn<DateTime>(
        name: "LastLoginAt",
        table: "Users",
        nullable: true);

    // CREATED AT DEFAULT FIX (IMPORTANT)
    migrationBuilder.Sql(@"
        UPDATE `Users`
        SET `CreatedAt` = CURRENT_TIMESTAMP
        WHERE `CreatedAt` = '0001-01-01 00:00:00';
    ");
}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
