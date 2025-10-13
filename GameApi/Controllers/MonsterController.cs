using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonstersController : ControllerBase
    {
        private readonly ILogger<MonstersController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly string _jsonPath;

        public MonstersController(ILogger<MonstersController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
            _jsonPath = Path.Combine(_env.ContentRootPath, "Database", "2014", "5e-SRD-Monsters.json");
        }

        private List<Monster> LoadMonstersFromJsonFile()
        {
            if (!System.IO.File.Exists(_jsonPath))
                throw new InvalidOperationException($"Monsters JSON file not found at: {_jsonPath}");

            var jsonString = System.IO.File.ReadAllText(_jsonPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new FlexibleIntConverter() }
            };

            return JsonSerializer.Deserialize<List<Monster>>(jsonString, options) ?? new List<Monster>();
        }

        // =========================
        // === Standard Endpoints
        // =========================
        [HttpGet]
        public IActionResult GetAll() => Ok(LoadMonstersFromJsonFile());

        [HttpGet("{index}")]
        public IActionResult GetByIndex(string index)
        {
            var monster = LoadMonstersFromJsonFile()
                .FirstOrDefault(m => m.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (monster == null) return NotFound($"Monster with index '{index}' not found.");
            return Ok(monster);
        }

        // =========================
        // === Additional Endpoints
        // =========================

        [HttpGet("size/{size}")]
        public IActionResult GetBySize(string size)
        {
            var monsters = LoadMonstersFromJsonFile()
                .Where(m => m.Size.Equals(size, StringComparison.OrdinalIgnoreCase));
            return Ok(monsters);
        }

        [HttpGet("type/{type}")]
        public IActionResult GetByType(string type)
        {
            var monsters = LoadMonstersFromJsonFile()
                .Where(m => m.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
            return Ok(monsters);
        }

        [HttpGet("alignment/{alignment}")]
        public IActionResult GetByAlignment(string alignment)
        {
            var monsters = LoadMonstersFromJsonFile()
                .Where(m => m.Alignment.Equals(alignment, StringComparison.OrdinalIgnoreCase));
            return Ok(monsters);
        }

        [HttpGet("challenge/{min}/{max}")]
        public IActionResult GetByChallengeRating(double min, double max)
        {
            var monsters = LoadMonstersFromJsonFile()
                .Where(m => m.ChallengeRating >= min && m.ChallengeRating <= max);
            return Ok(monsters);
        }

        [HttpGet("language/{language}")]
        public IActionResult GetByLanguage(string language)
        {
            var monsters = LoadMonstersFromJsonFile()
                .Where(m => m.Languages.Contains(language, StringComparison.OrdinalIgnoreCase));
            return Ok(monsters);
        }

        [HttpGet("has-ability/{abilityName}")]
        public IActionResult GetBySpecialAbility(string abilityName)
        {
            var monsters = LoadMonstersFromJsonFile()
                .Where(m => m.SpecialAbilities.Any(sa => sa.Name.Contains(abilityName, StringComparison.OrdinalIgnoreCase)));
            return Ok(monsters);
        }

        [HttpGet("has-action/{actionName}")]
        public IActionResult GetByAction(string actionName)
        {
            var monsters = LoadMonstersFromJsonFile()
                .Where(m => m.Actions.Any(a => a.Name.Contains(actionName, StringComparison.OrdinalIgnoreCase)));
            return Ok(monsters);
        }

        [HttpGet("proficiency/{proficiencyName}")]
        public IActionResult GetByProficiency(string proficiencyName)
        {
            var monsters = LoadMonstersFromJsonFile()
                .Where(m => m.Proficiencies.Any(p => p.Proficiency.Name.Contains(proficiencyName, StringComparison.OrdinalIgnoreCase)));
            return Ok(monsters);
        }

        [HttpGet("resistant-to/{damageType}")]
        public IActionResult GetByDamageResistance(string damageType)
        {
            var monsters = LoadMonstersFromJsonFile()
                .Where(m => m.DamageResistances.Any(d => d.Name.Equals(damageType, StringComparison.OrdinalIgnoreCase)));
            return Ok(monsters);
        }

        [HttpGet("immune-to/{condition}")]
        public IActionResult GetByConditionImmunity(string condition)
        {
            var monsters = LoadMonstersFromJsonFile()
                .Where(m => m.ConditionImmunities.Any(c => c.Name.Equals(condition, StringComparison.OrdinalIgnoreCase)));
            return Ok(monsters);
        }

        // =========================
        // === Flexible Int Converter
        // =========================
        public class FlexibleIntConverter : JsonConverter<int?>
        {
            public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        return reader.GetInt32();
                    case JsonTokenType.String:
                        var str = reader.GetString();
                        if (int.TryParse(str, out int val)) return val;
                        return null;
                    case JsonTokenType.True: return 1;
                    case JsonTokenType.False: return 0;
                    default: return null;
                }
            }

            public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
            {
                if (value.HasValue) writer.WriteNumberValue(value.Value);
                else writer.WriteNullValue();
            }
        }
        // =========================
        // === Monster Model
        // =========================
        public class Monster
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Desc { get; set; }
            public string Size { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string? Subtype { get; set; }
            public string Alignment { get; set; } = string.Empty;
            public List<ArmorClass> ArmorClass { get; set; } = new();
            public int HitPoints { get; set; }
            public string HitDice { get; set; } = string.Empty;
            public string? HitPointsRoll { get; set; }
            public Speed Speed { get; set; } = new();
            public int Strength { get; set; }
            public int Dexterity { get; set; }
            public int Constitution { get; set; }
            public int Intelligence { get; set; }
            public int Wisdom { get; set; }
            public int Charisma { get; set; }
            public List<MonsterProficiency> Proficiencies { get; set; } = new();
            public List<NamedAPIResource> DamageVulnerabilities { get; set; } = new();
            public List<NamedAPIResource> DamageResistances { get; set; } = new();
            public List<NamedAPIResource> DamageImmunities { get; set; } = new();
            public List<NamedAPIResource> ConditionImmunities { get; set; } = new();
            public Senses Senses { get; set; } = new();
            public string Languages { get; set; } = string.Empty;
            public double ChallengeRating { get; set; }
            public int ProficiencyBonus { get; set; }
            public int Xp { get; set; }
            public List<SpecialAbility> SpecialAbilities { get; set; } = new();
            public List<Action> Actions { get; set; } = new();
            public List<LegendaryAction>? LegendaryActions { get; set; }
            public string? Image { get; set; }
            public string Url { get; set; } = string.Empty;
        }

        public class ArmorClass
        {
            public string Type { get; set; } = string.Empty;

            [JsonConverter(typeof(FlexibleIntConverter))]
            public int? Value { get; set; }
        }

        public class Speed
        {
            public string? Walk { get; set; }
            public string? Fly { get; set; }
            public string? Swim { get; set; }
        }

        public class MonsterProficiency
        {
            public int Value { get; set; }
            public Proficiency Proficiency { get; set; } = new();
        }

        public class Proficiency
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

        public class Senses
        {
            public string? Darkvision { get; set; }
            public string? Blindsight { get; set; }
            public int PassivePerception { get; set; }
        }

        public class SpecialAbility
        {
            public string Name { get; set; } = string.Empty;
            public string Desc { get; set; } = string.Empty;
            public DC? Dc { get; set; }
            public Usage? Usage { get; set; }
            public Spellcasting? Spellcasting { get; set; }
        }

        public class DC
        {
            public DCType DcType { get; set; } = new();
            public int DcValue { get; set; }
            public string SuccessType { get; set; } = string.Empty;
        }

        public class DCType
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class Usage
        {
            public string Type { get; set; } = string.Empty;
            public int Times { get; set; }
        }

        public class Spellcasting
        {
            public int Level { get; set; }
            public Ability Ability { get; set; } = new();
            public int Dc { get; set; }
            public int Modifier { get; set; }
            public List<string> ComponentsRequired { get; set; } = new();
            public string School { get; set; } = string.Empty;
            public Dictionary<string, int> Slots { get; set; } = new();
            public List<Spell> Spells { get; set; } = new();
        }

        public class Ability
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class Spell
        {
            public string Name { get; set; } = string.Empty;
            public int Level { get; set; }
            public string Url { get; set; } = string.Empty;
        }

        public class Action
        {
            public string Name { get; set; } = string.Empty;
            public string? MultiattackType { get; set; }
            public string Desc { get; set; } = string.Empty;
            public int? AttackBonus { get; set; }
            public DC? Dc { get; set; }
            public List<Damage>? Damage { get; set; }
            public List<SubAction>? Actions { get; set; }
        }

        public class LegendaryAction
        {
            public string Name { get; set; } = string.Empty;
            public string Desc { get; set; } = string.Empty;
            public DC? Dc { get; set; }
            public List<Damage>? Damage { get; set; }
        }

        public class Damage
        {
            public NamedAPIResource DamageType { get; set; } = new();
            public string DamageDice { get; set; } = string.Empty;
        }

        public class SubAction
        {
            public string ActionName { get; set; } = string.Empty;

            [JsonConverter(typeof(FlexibleIntConverter))]
            public int? Count { get; set; }

            public string Type { get; set; } = string.Empty;
        }
    }
}

