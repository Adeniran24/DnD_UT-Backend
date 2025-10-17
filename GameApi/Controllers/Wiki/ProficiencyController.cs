// Controllers/ProficienciesController.cs
using Microsoft.AspNetCore.Mvc;
using DnDAPI.Models;
using System.Text.Json;

namespace DnDAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProficienciesController : ControllerBase
    {
        private readonly List<Proficiency> _proficiencies;
        private readonly ILogger<ProficienciesController> _logger;

        public ProficienciesController(ILogger<ProficienciesController> logger)
        {
            _logger = logger;
            _proficiencies = LoadProficienciesFromFile();
        }

        private List<Proficiency> LoadProficienciesFromFile()
        {
            try
            {
                // Fixed path to your JSON file
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "2014", "5e-SRD-Proficiencies.json");
                
                _logger.LogInformation("Looking for proficiencies file at: {FilePath}", filePath);
                
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("Proficiencies file not found at: {FilePath}", filePath);
                    return new List<Proficiency>();
                }

                var jsonData = System.IO.File.ReadAllText(filePath);
                _logger.LogInformation("Successfully loaded JSON data, length: {Length}", jsonData.Length);

                var proficiencies = JsonSerializer.Deserialize<List<Proficiency>>(jsonData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Deserialized {Count} proficiencies", proficiencies?.Count ?? 0);
                return proficiencies ?? new List<Proficiency>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading proficiencies from file");
                return new List<Proficiency>();
            }
        }

        // GET: api/proficiencies
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Proficiency>> GetAllProficiencies()
        {
            _logger.LogInformation("Returning {Count} proficiencies", _proficiencies.Count);
            return Ok(_proficiencies);
        }

        // GET: api/proficiencies/{index}
        [HttpGet("{index}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Proficiency> GetProficiency(string index)
        {
            var proficiency = _proficiencies.FirstOrDefault(p => 
                p.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (proficiency == null)
            {
                return NotFound($"Proficiency with index '{index}' not found.");
            }

            return Ok(proficiency);
        }

        // GET: api/proficiencies/type/{type}
        [HttpGet("type/{type}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Proficiency>> GetProficienciesByType(string type)
        {
            var proficiencies = _proficiencies.Where(p => 
                p.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(proficiencies);
        }

        // GET: api/proficiencies/class/{classIndex}
        [HttpGet("class/{classIndex}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Proficiency>> GetProficienciesByClass(string classIndex)
        {
            var proficiencies = _proficiencies.Where(p => 
                p.Classes.Any(c => c.Index.Equals(classIndex, StringComparison.OrdinalIgnoreCase))).ToList();

            return Ok(proficiencies);
        }

        // GET: api/proficiencies/race/{raceIndex}
        [HttpGet("race/{raceIndex}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Proficiency>> GetProficienciesByRace(string raceIndex)
        {
            var proficiencies = _proficiencies.Where(p => 
                p.Races.Any(r => r.Index.Equals(raceIndex, StringComparison.OrdinalIgnoreCase))).ToList();

            return Ok(proficiencies);
        }

        // GET: api/proficiencies/search
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<IEnumerable<Proficiency>> SearchProficiencies([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name parameter is required.");
            }

            var proficiencies = _proficiencies.Where(p => 
                p.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(proficiencies);
        }

        // GET: api/proficiencies/categories
        [HttpGet("categories")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<string>> GetProficiencyCategories()
        {
            var categories = _proficiencies
                .Select(p => p.Type)
                .Distinct()
                .Where(t => !string.IsNullOrEmpty(t))
                .OrderBy(t => t)
                .ToList();

            return Ok(categories);
        }
    }
}