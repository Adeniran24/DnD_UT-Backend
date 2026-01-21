using GameApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
//
namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CharactersController : ControllerBase
    {

        private static string NormalizeJson(string? value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            try
            {
                using var _ = JsonDocument.Parse(value);
                return value;
            }
            catch
            {
                return fallback;
            }
        }
        private readonly AppDbContext _context;
        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public CharactersController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET ALL CHARACTERS
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = Me;
            var characters = await _context.Characters
                .Where(c => c.userId == userId)
                .ToListAsync();

            return Ok(characters);
        }

        // =========================
        // GET BY ID
        // =========================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = Me;
            var character = await _context.Characters
                .FirstOrDefaultAsync(c => c.id == id && c.userId == userId);

            if (character == null) return NotFound();
            return Ok(character);
        }

        // =========================
        // CREATE
        // =========================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GameApi.Models.Character character)
        {
            if (character == null)
            {
                return BadRequest("Character payload is required.");
            }

            var userId = Me;
            character.id = 0;
            character.userId = userId;
            character.equipment = NormalizeJson(character.equipment, "{}");
            character.attacks = NormalizeJson(character.attacks, "[]");
            character.spellbook = NormalizeJson(character.spellbook, "[]");
            character.featuresFeats = NormalizeJson(character.featuresFeats, "[]");
            character.created_at = DateTime.UtcNow;
            character.updated_at = DateTime.UtcNow;

            _context.Characters.Add(character);
            await _context.SaveChangesAsync();

            return Ok(character);
        }

        // =========================
        // UPDATE
        // =========================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] GameApi.Models.Character updated)
        {
            if (updated == null)
            {
                return BadRequest("Character payload is required.");
            }

            var userId = Me;
            var existing = await _context.Characters
                .FirstOrDefaultAsync(c => c.id == id && c.userId == userId);

            if (existing == null) return NotFound();

            updated.id = existing.id;
            updated.userId = existing.userId;
            updated.created_at = existing.created_at;
            updated.equipment = NormalizeJson(updated.equipment, "{}");
            updated.attacks = NormalizeJson(updated.attacks, "[]");
            updated.spellbook = NormalizeJson(updated.spellbook, "[]");
            updated.featuresFeats = NormalizeJson(updated.featuresFeats, "[]");
            updated.updated_at = DateTime.UtcNow;

            _context.Entry(existing).CurrentValues.SetValues(updated);

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // =========================
        // DELETE
        // =========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = Me;
            var character = await _context.Characters
                .FirstOrDefaultAsync(c => c.id == id && c.userId == userId);

            if (character == null) return NotFound();

            _context.Characters.Remove(character);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

// ==========================================================
// MODEL (UGYANEBBEN A FÁJLBAN)
// ==========================================================

namespace GameApi.Models
{
    [Table("characters")]
    public class Character
    {
        [Key]
        public int id { get; set; }

        public int? userId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(userId))]
        public User? User { get; set; }

        public string characterName { get; set; } = "";
        public string classLevel { get; set; } = "";
        public string race { get; set; } = "";
        public string background { get; set; } = "";
        public string playerName { get; set; } = "";
        public string alignment { get; set; } = "";

        public int xp { get; set; }
        public int inspiration { get; set; }

        public int age { get; set; }
        public string height { get; set; } = "";
        public string weight { get; set; } = "";
        public string eyes { get; set; } = "";
        public string skin { get; set; } = "";
        public string hair { get; set; } = "";
        public string symbol { get; set; } = "";
        public string appearance { get; set; } = "";

        public int str { get; set; }
        public int dex { get; set; }
        public int con { get; set; }

        [Column("int_stat")]
        public int int_stat { get; set; }

        public int wis { get; set; }
        public int cha { get; set; }

        public int profBonus { get; set; }
        public int profBonusDuplicate { get; set; }

        public bool saveProf_str { get; set; }
        public bool saveProf_dex { get; set; }
        public bool saveProf_con { get; set; }
        public bool saveProf_int { get; set; }
        public bool saveProf_wis { get; set; }
        public bool saveProf_cha { get; set; }

        public bool skillProf_acrobatics { get; set; }
        public bool skillProf_animalHandling { get; set; }
        public bool skillProf_arcana { get; set; }
        public bool skillProf_athletics { get; set; }
        public bool skillProf_deception { get; set; }
        public bool skillProf_history { get; set; }
        public bool skillProf_insight { get; set; }
        public bool skillProf_intimidation { get; set; }
        public bool skillProf_investigation { get; set; }
        public bool skillProf_medicine { get; set; }
        public bool skillProf_nature { get; set; }
        public bool skillProf_perception { get; set; }
        public bool skillProf_performance { get; set; }
        public bool skillProf_persuasion { get; set; }
        public bool skillProf_religion { get; set; }
        public bool skillProf_sleightOfHand { get; set; }
        public bool skillProf_stealth { get; set; }
        public bool skillProf_survival { get; set; }

        public bool skillExp_acrobatics { get; set; }
        public bool skillExp_animalHandling { get; set; }
        public bool skillExp_arcana { get; set; }
        public bool skillExp_athletics { get; set; }
        public bool skillExp_deception { get; set; }
        public bool skillExp_history { get; set; }
        public bool skillExp_insight { get; set; }
        public bool skillExp_intimidation { get; set; }
        public bool skillExp_investigation { get; set; }
        public bool skillExp_medicine { get; set; }
        public bool skillExp_nature { get; set; }
        public bool skillExp_perception { get; set; }
        public bool skillExp_performance { get; set; }
        public bool skillExp_persuasion { get; set; }
        public bool skillExp_religion { get; set; }
        public bool skillExp_sleightOfHand { get; set; }
        public bool skillExp_stealth { get; set; }
        public bool skillExp_survival { get; set; }

        public int armor { get; set; }
        public int initiative { get; set; }
        public int speed { get; set; }

        public int hpMax { get; set; }
        public int hpCurrent { get; set; }
        public int hpTemp { get; set; }

        public int hitDiceTotal { get; set; }
        public int hitDiceCurrent { get; set; }

        public int deathSuccesses { get; set; }
        public int deathFailures { get; set; }

        public int passivePerception { get; set; }

        public int cp { get; set; }
        public int sp { get; set; }
        public int ep { get; set; }
        public int gp { get; set; }
        public int pp { get; set; }

        public string otherProfs { get; set; } = "";
        public string personalityTraits { get; set; } = "";
        public string ideals { get; set; } = "";
        public string bonds { get; set; } = "";
        public string flaws { get; set; } = "";
        public string allies { get; set; } = "";
        public string additionalFeatures { get; set; } = "";
        public string treasure { get; set; } = "";
        public string backstory { get; set; } = "";

        public string portraitDataUrl { get; set; } = "";

        public string equipment { get; set; } = "{}";
        public string attacks { get; set; } = "[]";
        public string spellbook { get; set; } = "[]";
        public string featuresFeats { get; set; } = "[]";

        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}


