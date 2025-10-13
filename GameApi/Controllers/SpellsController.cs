using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DnDAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpellsController : ControllerBase
    {
        private readonly ILogger<SpellsController> _logger;
        private readonly List<Spell> _spells;
        private readonly string _jsonPath;

        public SpellsController(ILogger<SpellsController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _jsonPath = Path.Combine(env.ContentRootPath, "Database", "2014", "5e-SRD-Spells.json");
            _spells = LoadSpellsFromFile();
        }

        // =========================
        // === Load JSON
        // =========================
        private List<Spell> LoadSpellsFromFile()
        {
            if (!System.IO.File.Exists(_jsonPath))
            {
                _logger.LogWarning("Spells file not found at: {FilePath}", _jsonPath);
                return new List<Spell>();
            }

            var jsonData = System.IO.File.ReadAllText(_jsonPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new FlexibleIntConverter() }
            };

            try
            {
                return JsonSerializer.Deserialize<List<Spell>>(jsonData, options) ?? new List<Spell>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing spells JSON");
                return new List<Spell>();
            }
        }

        // =========================
        // === Endpoints
        // =========================

        [HttpGet]
        public ActionResult<IEnumerable<Spell>> GetAll() => Ok(_spells);

        [HttpGet("{index}")]
        public ActionResult<Spell> GetByIndex(string index)
        {
            var spell = _spells.FirstOrDefault(s => s.Index.Equals(index, StringComparison.OrdinalIgnoreCase));
            return spell == null ? NotFound($"Spell '{index}' not found.") : Ok(spell);
        }

        [HttpGet("level/{level}")]
        public ActionResult<IEnumerable<Spell>> GetByLevel(int level)
        {
            var spells = _spells.Where(s => s.Level == level).ToList();
            return Ok(spells);
        }

        [HttpGet("school/{schoolIndex}")]
        public ActionResult<IEnumerable<Spell>> GetBySchool(string schoolIndex)
        {
            var spells = _spells.Where(s => s.School.Index.Equals(schoolIndex, StringComparison.OrdinalIgnoreCase));
            return Ok(spells);
        }

        [HttpGet("classes/{classIndex}")]
        public ActionResult<IEnumerable<Spell>> GetByClass(string classIndex)
        {
            var spells = _spells.Where(s => s.Classes.Any(c => c.Index.Equals(classIndex, StringComparison.OrdinalIgnoreCase)));
            return Ok(spells);
        }

        [HttpGet("subclasses/{subclassIndex}")]
        public ActionResult<IEnumerable<Spell>> GetBySubclass(string subclassIndex)
        {
            var spells = _spells.Where(s => s.Subclasses.Any(c => c.Index.Equals(subclassIndex, StringComparison.OrdinalIgnoreCase)));
            return Ok(spells);
        }

        [HttpGet("ritual")]
        public ActionResult<IEnumerable<Spell>> GetRituals()
        {
            var spells = _spells.Where(s => s.Ritual == 1).ToList();
            return Ok(spells);
        }

        [HttpGet("concentration")]
        public ActionResult<IEnumerable<Spell>> GetConcentrationSpells()
        {
            var spells = _spells.Where(s => s.Concentration == 1).ToList();
            return Ok(spells);
        }

        [HttpGet("search")]
        public ActionResult<IEnumerable<Spell>> SearchByName([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Name is required.");
            var spells = _spells.Where(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
            return Ok(spells);
        }

        // =========================
        // === Models
        // =========================
        public class Spell
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public List<string> Desc { get; set; } = new();
            public List<string>? HigherLevel { get; set; }
            public string Range { get; set; } = string.Empty;
            public List<string> Components { get; set; } = new();
            public string? Material { get; set; }
            [JsonConverter(typeof(FlexibleIntConverter))] public int Ritual { get; set; }
            public string Duration { get; set; } = string.Empty;
            [JsonConverter(typeof(FlexibleIntConverter))] public int Concentration { get; set; }
            public string CastingTime { get; set; } = string.Empty;
            [JsonConverter(typeof(FlexibleIntConverter))] public int Level { get; set; }
            public string? AttackType { get; set; }
            public Damage? Damage { get; set; }
            public DC? Dc { get; set; }
            public School School { get; set; } = new();
            public List<NamedAPIResource> Classes { get; set; } = new();
            public List<NamedAPIResource> Subclasses { get; set; } = new();
            public Dictionary<string, string>? DamageAtSlotLevel { get; set; }
            public Dictionary<string, string>? DamageAtCharacterLevel { get; set; }
            public Dictionary<string, string>? HealAtSlotLevel { get; set; }
            public string Url { get; set; } = string.Empty;
        }

        public class Damage
        {
            public NamedAPIResource DamageType { get; set; } = new();
            public Dictionary<string, string>? DamageAtSlotLevel { get; set; }
            public Dictionary<string, string>? DamageAtCharacterLevel { get; set; }
        }

        public class DC
        {
            public DCType DcType { get; set; } = new();
            public string DcSuccess { get; set; } = string.Empty;
        }

        public class DCType
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class School
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class NamedAPIResource
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        // =========================
        // === Flexible Int Converter
        // =========================
        public class FlexibleIntConverter : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        return reader.GetInt32();
                    case JsonTokenType.String:
                        var str = reader.GetString();
                        if (int.TryParse(str, out int val))
                            return val;
                        return 0;
                    case JsonTokenType.True: return 1;
                    case JsonTokenType.False: return 0;
                    default: return 0;
                }
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }
    }
}
