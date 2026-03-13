using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApi.Migrations
{
    public partial class AddSkillExpertiseToCharacters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // skillExp_* columns were removed from the model to match the current database schema.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
