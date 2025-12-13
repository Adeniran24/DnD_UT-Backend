using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAndAdminFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Users_UserId",
                table: "Characters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Characters",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_UserId",
                table: "Characters");

            migrationBuilder.RenameTable(
                name: "Characters",
                newName: "characters");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "characters",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "characters",
                newName: "xp");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "characters",
                newName: "weight");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "additionalFeatures",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "age",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "alignment",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "allies",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "appearance",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "armor",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "attacks",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "background",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "backstory",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "bonds",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "cha",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "characterName",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "classLevel",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "con",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "cp",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "characters",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "deathFailures",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "deathSuccesses",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "dex",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ep",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "equipment",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "eyes",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "featuresFeats",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "flaws",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "gp",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "hair",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "height",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "hitDiceCurrent",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "hitDiceTotal",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "hpCurrent",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "hpMax",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "hpTemp",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ideals",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "initiative",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "inspiration",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "int_stat",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "otherProfs",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "passivePerception",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "personalityTraits",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "playerName",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "portraitDataUrl",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "pp",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "profBonus",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "profBonusDuplicate",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "race",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "saveProf_cha",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "saveProf_con",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "saveProf_dex",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "saveProf_int",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "saveProf_str",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "saveProf_wis",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_acrobatics",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_animalHandling",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_arcana",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_athletics",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_deception",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_history",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_insight",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_intimidation",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_investigation",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_medicine",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_nature",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_perception",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_performance",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_persuasion",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_religion",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_sleightOfHand",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_stealth",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "skillProf_survival",
                table: "characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "skin",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "sp",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "speed",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "spellbook",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "str",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "symbol",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "treasure",
                table: "characters",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "characters",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "wis",
                table: "characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_characters",
                table: "characters",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_characters",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "additionalFeatures",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "age",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "alignment",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "allies",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "appearance",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "armor",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "attacks",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "background",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "backstory",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "bonds",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "cha",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "characterName",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "classLevel",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "con",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "cp",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "deathFailures",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "deathSuccesses",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "dex",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "ep",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "equipment",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "eyes",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "featuresFeats",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "flaws",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "gp",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "hair",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "height",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "hitDiceCurrent",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "hitDiceTotal",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "hpCurrent",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "hpMax",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "hpTemp",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "ideals",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "initiative",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "inspiration",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "int_stat",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "otherProfs",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "passivePerception",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "personalityTraits",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "playerName",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "portraitDataUrl",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "pp",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "profBonus",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "profBonusDuplicate",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "race",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "saveProf_cha",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "saveProf_con",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "saveProf_dex",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "saveProf_int",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "saveProf_str",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "saveProf_wis",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_acrobatics",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_animalHandling",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_arcana",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_athletics",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_deception",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_history",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_insight",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_intimidation",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_investigation",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_medicine",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_nature",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_perception",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_performance",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_persuasion",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_religion",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_sleightOfHand",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_stealth",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skillProf_survival",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skin",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "sp",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "speed",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "spellbook",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "str",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "symbol",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "treasure",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "wis",
                table: "characters");

            migrationBuilder.RenameTable(
                name: "characters",
                newName: "Characters");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Characters",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "xp",
                table: "Characters",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "weight",
                table: "Characters",
                newName: "Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Characters",
                table: "Characters",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId",
                table: "Characters",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Users_UserId",
                table: "Characters",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
