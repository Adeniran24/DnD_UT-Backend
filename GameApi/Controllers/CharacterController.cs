using GameApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameApi.Models;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CharactersController : ControllerBase
    {
        private readonly AppDbContext _context;

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
            return Ok(await _context.Characters.ToListAsync());
        }

        // =========================
        // GET BY ID
        // =========================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var character = await _context.Characters.FindAsync(id);
            if (character == null) return NotFound();
            return Ok(character);
        }

        // =========================
        // CREATE
        // =========================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Character character)
        {
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
            var existing = await _context.Characters.FindAsync(id);
            if (existing == null) return NotFound();

            _context.Entry(existing).CurrentValues.SetValues(updated);
            existing.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // =========================
        // DELETE
        // =========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var character = await _context.Characters.FindAsync(id);
            if (character == null) return NotFound();

            _context.Characters.Remove(character);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // ==========================================================
    // MODEL
}