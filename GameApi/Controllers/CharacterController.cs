using GameApi.Data;
using GameApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CharactersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public CharactersController(AppDbContext context)
        {
            _context = context;
        }

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
        public async Task<IActionResult> Create([FromBody] Character character)
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
        public async Task<IActionResult> Update(int id, [FromBody] Character updated)
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
