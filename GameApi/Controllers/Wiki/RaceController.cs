// Controllers/RacesController.cs
using Microsoft.AspNetCore.Mvc;
using DnDAPI.Models;
using System.Text.Json;

namespace DnDAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RacesController : ControllerBase
    {
        private readonly List<Race> _races;
        private readonly ILogger<RacesController> _logger;

        public RacesController(ILogger<RacesController> logger)
        {
            _logger = logger;
            _races = LoadRacesFromFile();
        }

        private List<Race> LoadRacesFromFile()
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "2014", "5e-SRD-Races.json");
                
                _logger.LogInformation("Looking for races file at: {FilePath}", filePath);
                
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("Races file not found at: {FilePath}", filePath);
                    return new List<Race>();
                }

                var jsonData = System.IO.File.ReadAllText(filePath);
                _logger.LogInformation("Successfully loaded races JSON data, length: {Length}", jsonData.Length);

                var races = JsonSerializer.Deserialize<List<Race>>(jsonData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Deserialized {Count} races", races?.Count ?? 0);
                return races ?? new List<Race>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading races from file");
                return new List<Race>();
            }
        }

        // GET: api/races
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Race>> GetAllRaces()
        {
            _logger.LogInformation("Returning {Count} races", _races.Count);
            return Ok(_races);
        }

        // GET: api/races/{index}
        [HttpGet("{index}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Race> GetRace(string index)
        {
            var race = _races.FirstOrDefault(r => 
                r.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (race == null)
            {
                return NotFound($"Race with index '{index}' not found.");
            }

            return Ok(race);
        }

        // GET: api/races/{index}/traits
        [HttpGet("{index}/traits")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<TraitReference>> GetRaceTraits(string index)
        {
            var race = _races.FirstOrDefault(r => 
                r.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (race == null)
            {
                return NotFound($"Race with index '{index}' not found.");
            }

            return Ok(race.Traits);
        }

        // GET: api/races/{index}/subraces
        [HttpGet("{index}/subraces")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<SubraceReference>> GetRaceSubraces(string index)
        {
            var race = _races.FirstOrDefault(r => 
                r.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (race == null)
            {
                return NotFound($"Race with index '{index}' not found.");
            }

            return Ok(race.Subraces);
        }

        // GET: api/races/{index}/languages
        [HttpGet("{index}/languages")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<LanguageReference>> GetRaceLanguages(string index)
        {
            var race = _races.FirstOrDefault(r => 
                r.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (race == null)
            {
                return NotFound($"Race with index '{index}' not found.");
            }

            return Ok(race.Languages);
        }

        // GET: api/races/{index}/ability-bonuses
        [HttpGet("{index}/ability-bonuses")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<AbilityBonus>> GetRaceAbilityBonuses(string index)
        {
            var race = _races.FirstOrDefault(r => 
                r.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (race == null)
            {
                return NotFound($"Race with index '{index}' not found.");
            }

            return Ok(race.AbilityBonuses);
        }

        // GET: api/races/search?name={name}
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<IEnumerable<Race>> SearchRaces([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name parameter is required.");
            }

            var races = _races.Where(r => 
                r.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(races);
        }

        // GET: api/races/sizes
        [HttpGet("sizes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<string>> GetRaceSizes()
        {
            var sizes = _races
                .Select(r => r.Size)
                .Distinct()
                .Where(s => !string.IsNullOrEmpty(s))
                .OrderBy(s => s)
                .ToList();

            return Ok(sizes);
        }

        // GET: api/races/speed/{minSpeed}
        [HttpGet("speed/{minSpeed}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Race>> GetRacesBySpeed(int minSpeed)
        {
            var races = _races.Where(r => r.Speed >= minSpeed).ToList();
            return Ok(races);
        }
    }
}