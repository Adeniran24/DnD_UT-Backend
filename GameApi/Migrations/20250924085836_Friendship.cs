using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApi.Migrations
{
    /// <inheritdoc />
    public partial class Friendship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
    "ALTER TABLE `Friendships` CHANGE COLUMN `CreatedAt` `RequestedAt` DATETIME NOT NULL;");

            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                table: "Friendships",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "Friendships");

            migrationBuilder.RenameColumn(
                name: "RequestedAt",
                table: "Friendships",
                newName: "CreatedAt");
        }
    }
}
