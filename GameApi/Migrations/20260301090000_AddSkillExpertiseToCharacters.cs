using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApi.Migrations
{
    public partial class AddSkillExpertiseToCharacters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "skillExp_acrobatics",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_animalHandling",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_arcana",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_athletics",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_deception",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_history",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_insight",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_intimidation",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_investigation",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_medicine",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_nature",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_perception",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_performance",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_persuasion",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_religion",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_sleightOfHand",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_stealth",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillExp_survival",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "skillExp_acrobatics", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_animalHandling", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_arcana", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_athletics", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_deception", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_history", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_insight", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_intimidation", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_investigation", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_medicine", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_nature", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_perception", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_performance", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_persuasion", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_religion", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_sleightOfHand", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_stealth", table: "characters");
            migrationBuilder.DropColumn(name: "skillExp_survival", table: "characters");
        }
    }
}
