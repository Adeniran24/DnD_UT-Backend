using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameApi.Models;

namespace GameApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonstersController : ControllerBase
    {
        private readonly ILogger<MonstersController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly string _jsonPath;
        private static List<Monster>? _cachedMonsters;
        private static readonly object _cacheLock = new();

        public MonstersController(ILogger<MonstersController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
            _jsonPath = Path.Combine(_env.ContentRootPath, "Database", "2014", "5e-SRD-Monsters.json");
        }

        // =========================
        // === Utility Methods
        // =========================

        private List<Monster> LoadMonsters()
        {
            if (_cachedMonsters != null)
                return _cachedMonsters;

            lock (_cacheLock)
            {
                if (_cachedMonsters != null)
                    return _cachedMonsters;

                if (!System.IO.File.Exists(_jsonPath))
                    throw new FileNotFoundException($"Monsters JSON file not found at: {_jsonPath}");

                _logger.LogInformation("Loading monsters from JSON file: {path}", _jsonPath);
                var json = System.IO.File.ReadAllText(_jsonPath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new FlexibleIntConverter() }
                };

                _cachedMonsters = JsonSerializer.Deserialize<List<Monster>>(json, options)
                                 ?? new List<Monster>();

                _logger.LogInformation("Loaded {count} monsters into memory.", _cachedMonsters.Count);
                return _cachedMonsters;
            }
        }

        private IEnumerable<Monster> Filter(Func<Monster, bool> predicate)
            => LoadMonsters().Where(predicate);

        // =========================
        // === Basic Endpoints
        // =========================

        [HttpGet]
        public IActionResult GetAll()
        {
            var monsters = LoadMonsters();
            return Ok(monsters);
        }

        [HttpGet("{index}")]
        public IActionResult GetByIndex(string index)
        {
            var monster = LoadMonsters()
                .FirstOrDefault(m => string.Equals(m.Index, index, StringComparison.OrdinalIgnoreCase));

            return monster is null
                ? NotFound(new { message = $"Monster '{index}' not found." })
                : Ok(monster);
        }

        // =========================
        // === Filter Endpoints
        // =========================

        [HttpGet("size/{size}")]
        public IActionResult GetBySize(string size)
            => Ok(Filter(m => string.Equals(m.Size, size, StringComparison.OrdinalIgnoreCase)));

        [HttpGet("type/{type}")]
        public IActionResult GetByType(string type)
            => Ok(Filter(m => string.Equals(m.Type, type, StringComparison.OrdinalIgnoreCase)));

        [HttpGet("alignment/{alignment}")]
        public IActionResult GetByAlignment(string alignment)
            => Ok(Filter(m => string.Equals(m.Alignment, alignment, StringComparison.OrdinalIgnoreCase)));

        [HttpGet("challenge/{min:double}/{max:double}")]
        public IActionResult GetByChallengeRating(double min, double max)
            => Ok(Filter(m => m.ChallengeRating >= min && m.ChallengeRating <= max));

        [HttpGet("language/{language}")]
        public IActionResult GetByLanguage(string language)
            => Ok(Filter(m => m.Languages?.Contains(language, StringComparison.OrdinalIgnoreCase) == true));

        [HttpGet("has-ability/{abilityName}")]
        public IActionResult GetBySpecialAbility(string abilityName)
            => Ok(Filter(m => m.SpecialAbilities?.Any(sa => sa.Name.Contains(abilityName, StringComparison.OrdinalIgnoreCase)) == true));

        [HttpGet("has-action/{actionName}")]
        public IActionResult GetByAction(string actionName)
            => Ok(Filter(m => m.Actions?.Any(a => a.Name.Contains(actionName, StringComparison.OrdinalIgnoreCase)) == true));

        [HttpGet("proficiency/{proficiencyName}")]
        public IActionResult GetByProficiency(string proficiencyName)
            => Ok(Filter(m => m.Proficiencies?.Any(p =>
                    p.Proficiency?.Name?.Contains(proficiencyName, StringComparison.OrdinalIgnoreCase) == true) == true));

        [HttpGet("resistant-to/{damageType}")]
        public IActionResult GetByDamageResistance(string damageType)
            => Ok(Filter(m => m.DamageResistances?.Any(d =>
                    string.Equals(d, damageType, StringComparison.OrdinalIgnoreCase)) == true));

        [HttpGet("immune-to/{condition}")]
        public IActionResult GetByConditionImmunity(string condition)
            => Ok(Filter(m => m.ConditionImmunities?.Any(c =>
                    string.Equals(c.Name, condition, StringComparison.OrdinalIgnoreCase)) == true));

        // =========================
        // === Combined Filters
        // =========================

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string? name = null, [FromQuery] string? type = null, [FromQuery] string? size = null, [FromQuery] double? minCR = null, [FromQuery] double? maxCR = null)
        {
            var monsters = LoadMonsters().AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
                monsters = monsters.Where(m => m.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(type))
                monsters = monsters.Where(m => string.Equals(m.Type, type, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(size))
                monsters = monsters.Where(m => string.Equals(m.Size, size, StringComparison.OrdinalIgnoreCase));
            if (minCR.HasValue)
                monsters = monsters.Where(m => m.ChallengeRating >= minCR.Value);
            if (maxCR.HasValue)
                monsters = monsters.Where(m => m.ChallengeRating <= maxCR.Value);

            return Ok(monsters.ToList());
        }

        // =========================
        // === Maintenance
        // =========================

        [HttpPost("reload")]
        public IActionResult ReloadCache()
        {
            lock (_cacheLock)
            {
                _cachedMonsters = null;
            }

            var reloaded = LoadMonsters();
            return Ok(new { message = "Monster cache reloaded.", count = reloaded.Count });
        }

        // =========================
        // === Flexible Int Converter
        // =========================

        public class FlexibleIntConverter : JsonConverter<int?>
        {
            public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.TokenType switch
                {
                    JsonTokenType.Number => reader.GetInt32(),
                    JsonTokenType.String => int.TryParse(reader.GetString(), out var val) ? val : null,
                    JsonTokenType.True => 1,
                    JsonTokenType.False => 0,
                    _ => null
                };
            }

            public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
            {
                if (value.HasValue) writer.WriteNumberValue(value.Value);
                else writer.WriteNullValue();
            }
        }
    }
}
