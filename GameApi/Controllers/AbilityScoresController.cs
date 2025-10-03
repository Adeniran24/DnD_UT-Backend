using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using GameApi.DTOs.DND2014;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/2014/[controller]")]
    public class AbilityScoresController : ControllerBase
    {
        private static readonly List<AbilityScore> _abilityScores;

        // Static constructor to load JSON once
        static AbilityScoresController()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "2014", "5e-SRD-Ability-Scores.json");

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    _abilityScores = JsonSerializer.Deserialize<List<AbilityScore>>(jsonString) ?? new List<AbilityScore>();
                }
                catch
                {
                    _abilityScores = new List<AbilityScore>();
                }
            }
            else
            {
                _abilityScores = new List<AbilityScore>();
            }
        }

        // GET: api/2014/ability-scores
        [HttpGet]
        public IActionResult GetAll()
        {
            if (!_abilityScores.Any())
                return NotFound("Ability scores data not found.");

            return Ok(_abilityScores);
        }

        // GET: api/2014/ability-scores/{index}
        [HttpGet("{index}")]
        public IActionResult GetByIndex(string index)
        {
            var abilityScore = _abilityScores.FirstOrDefault(a =>
                a.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (abilityScore == null)
                return NotFound($"Ability score with index '{index}' not found.");

            return Ok(abilityScore);
        }
    }
}
