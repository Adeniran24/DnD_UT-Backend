using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApi.Migrations
{
    /// <inheritdoc />
    public partial class Uj : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DNDClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HitDie = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DNDClasses", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DNDMultiClassings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClassId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DNDMultiClassings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DNDMultiClassings_DNDClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "DNDClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DNDProficiencyChoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Desc = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Choose = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClassId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DNDProficiencyChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DNDProficiencyChoices_DNDClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "DNDClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DNDStartingEquipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DNDStartingEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DNDStartingEquipment_DNDClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "DNDClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DNDSubclasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClassId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DNDSubclasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DNDSubclasses_DNDClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "DNDClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DNDPrerequisites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AbilityScore = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MinimumScore = table.Column<int>(type: "int", nullable: false),
                    MultiClassingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DNDPrerequisites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DNDPrerequisites_DNDMultiClassings_MultiClassingId",
                        column: x => x.MultiClassingId,
                        principalTable: "DNDMultiClassings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DNDProficiencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    MultiClassingId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DNDProficiencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DNDProficiencies_DNDClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "DNDClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DNDProficiencies_DNDMultiClassings_MultiClassingId",
                        column: x => x.MultiClassingId,
                        principalTable: "DNDMultiClassings",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DNDMultiClassings_ClassId",
                table: "DNDMultiClassings",
                column: "ClassId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DNDPrerequisites_MultiClassingId",
                table: "DNDPrerequisites",
                column: "MultiClassingId");

            migrationBuilder.CreateIndex(
                name: "IX_DNDProficiencies_ClassId",
                table: "DNDProficiencies",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_DNDProficiencies_MultiClassingId",
                table: "DNDProficiencies",
                column: "MultiClassingId");

            migrationBuilder.CreateIndex(
                name: "IX_DNDProficiencyChoices_ClassId",
                table: "DNDProficiencyChoices",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_DNDStartingEquipment_ClassId",
                table: "DNDStartingEquipment",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_DNDSubclasses_ClassId",
                table: "DNDSubclasses",
                column: "ClassId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DNDPrerequisites");

            migrationBuilder.DropTable(
                name: "DNDProficiencies");

            migrationBuilder.DropTable(
                name: "DNDProficiencyChoices");

            migrationBuilder.DropTable(
                name: "DNDStartingEquipment");

            migrationBuilder.DropTable(
                name: "DNDSubclasses");

            migrationBuilder.DropTable(
                name: "DNDMultiClassings");

            migrationBuilder.DropTable(
                name: "DNDClasses");
        }
    }
}
