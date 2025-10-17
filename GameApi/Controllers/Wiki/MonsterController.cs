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
        private readonly List<Monster> _monsters;

        public MonstersController(ILogger<MonstersController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            var filePath = Path.Combine(env.ContentRootPath, "Database", "2014", "5e-SRD-Monsters.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
            };

            try
            {
                var json = System.IO.File.ReadAllText(filePath);
                _monsters = JsonSerializer.Deserialize<List<Monster>>(json, options) ?? new List<Monster>();
                _logger.LogInformation($"✅ Loaded {_monsters.Count} monsters from {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load monsters from JSON");
                _monsters = new List<Monster>();
            }
        }

        [HttpGet]
        public IActionResult GetAll() => Ok(_monsters);

        [HttpGet("{index}")]
        public IActionResult GetByIndex(string index)
        {
            var monster = _monsters.FirstOrDefault(m => string.Equals(m.Index, index, StringComparison.OrdinalIgnoreCase));
            if (monster == null)
                return NotFound(new { message = $"Monster with index '{index}' not found." });

            return Ok(monster);
        }
        
    }

    // 🔹 Flexible converters

    public class FlexibleStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString() ?? "",
                JsonTokenType.Number => reader.GetDouble().ToString(),
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                _ => ""
            };

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value);
    }

    public class FlexibleStringOrObjectConverter : JsonConverter<List<string>>
    {
        public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new List<string>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return list;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                if (reader.TokenType == JsonTokenType.String)
                {
                    list.Add(reader.GetString()!);
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    using var doc = JsonDocument.ParseValue(ref reader);
                    if (doc.RootElement.TryGetProperty("name", out var nameProp))
                        list.Add(nameProp.GetString() ?? "");
                }
            }
            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var s in value)
                writer.WriteStringValue(s);
            writer.WriteEndArray();
        }
    }

    // 🔹 Monster model

    public class Monster
    {
        [JsonPropertyName("index")] public string Index { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("desc")] public string? Desc { get; set; }
        [JsonPropertyName("size")] public string Size { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("subtype")] public string? Subtype { get; set; }
        [JsonPropertyName("alignment")] public string Alignment { get; set; } = "";

        [JsonPropertyName("armor_class")] public List<ArmorClass> ArmorClass { get; set; } = new();
        [JsonPropertyName("hit_points")] public int HitPoints { get; set; }
        [JsonPropertyName("hit_dice")] public string HitDice { get; set; } = "";
        [JsonPropertyName("hit_points_roll")] public string? HitPointsRoll { get; set; }
        [JsonPropertyName("speed")] public Speed? Speed { get; set; }

        [JsonPropertyName("strength")] public int Strength { get; set; }
        [JsonPropertyName("dexterity")] public int Dexterity { get; set; }
        [JsonPropertyName("constitution")] public int Constitution { get; set; }
        [JsonPropertyName("intelligence")] public int Intelligence { get; set; }
        [JsonPropertyName("wisdom")] public int Wisdom { get; set; }
        [JsonPropertyName("charisma")] public int Charisma { get; set; }

        [JsonPropertyName("proficiencies")] public List<Proficiency> Proficiencies { get; set; } = new();

        [JsonPropertyName("damage_vulnerabilities")]
        [JsonConverter(typeof(FlexibleStringOrObjectConverter))]
        public List<string> DamageVulnerabilities { get; set; } = new();

        [JsonPropertyName("damage_resistances")]
        [JsonConverter(typeof(FlexibleStringOrObjectConverter))]
        public List<string> DamageResistances { get; set; } = new();

        [JsonPropertyName("damage_immunities")]
        [JsonConverter(typeof(FlexibleStringOrObjectConverter))]
        public List<string> DamageImmunities { get; set; } = new();

        [JsonPropertyName("condition_immunities")]
        [JsonConverter(typeof(FlexibleStringOrObjectConverter))]
        public List<string> ConditionImmunities { get; set; } = new();

        [JsonPropertyName("senses")] public Senses? Senses { get; set; }

        [JsonPropertyName("languages")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string Languages { get; set; } = "";

        [JsonPropertyName("challenge_rating")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string ChallengeRating { get; set; } = "";

        [JsonPropertyName("proficiency_bonus")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string ProficiencyBonus { get; set; } = "";

        [JsonPropertyName("xp")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string Xp { get; set; } = "";

        [JsonPropertyName("special_abilities")] public List<ActionInfo> SpecialAbilities { get; set; } = new();
        [JsonPropertyName("actions")] public List<ActionInfo> Actions { get; set; } = new();
        [JsonPropertyName("legendary_actions")] public List<ActionInfo> LegendaryActions { get; set; } = new();
        [JsonPropertyName("reactions")] public List<ActionInfo> Reactions { get; set; } = new();

        [JsonPropertyName("image")] public string? Image { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
    }

    // 🔹 Other models

    public class ArmorClass
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("value")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string Value { get; set; } = "";
    }

    public class Speed
    {
        [JsonPropertyName("walk")] public string? Walk { get; set; }
        [JsonPropertyName("swim")] public string? Swim { get; set; }
        [JsonPropertyName("fly")] public string? Fly { get; set; }
        [JsonPropertyName("burrow")] public string? Burrow { get; set; }
        [JsonPropertyName("climb")] public string? Climb { get; set; }
    }

    public class Proficiency
    {
        [JsonPropertyName("proficiency")] public ProficiencyRef ProficiencyRef { get; set; } = new();
        [JsonPropertyName("value")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string Value { get; set; } = "";
    }

    public class ProficiencyRef
    {
        [JsonPropertyName("index")] public string Index { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("url")] public string Url { get; set; } = "";
    }

    public class Senses
    {
        [JsonPropertyName("passive_perception")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string PassivePerception { get; set; } = "";

        [JsonPropertyName("blindsight")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? Blindsight { get; set; }

        [JsonPropertyName("darkvision")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? Darkvision { get; set; }
    }

    public class ActionInfo
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("desc")] public string? Desc { get; set; }
        [JsonPropertyName("actions")] public List<SubAction> Actions { get; set; } = new();
        [JsonPropertyName("damage")] public List<Damage> Damage { get; set; } = new();
        [JsonPropertyName("dc")] public Dc? Dc { get; set; }
        [JsonPropertyName("attack_bonus")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? AttackBonus { get; set; }
        [JsonPropertyName("usage")] public object? Usage { get; set; }
    }

    public class SubAction
    {
        [JsonPropertyName("action_name")] public string ActionName { get; set; } = "";
        [JsonPropertyName("count")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string Count { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
    }

    public class Damage
    {
        [JsonPropertyName("damage_type")] public DamageInfo DamageType { get; set; } = new();
        [JsonPropertyName("damage_dice")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string DiceRoll { get; set; } = "";
    }

    public class DamageInfo
    {
        [JsonPropertyName("index")] public string Index { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("url")] public string Url { get; set; } = "";
    }

    public class Dc
    {
        [JsonPropertyName("dc_value")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string DcValue { get; set; } = "";
        [JsonPropertyName("dc_type")] public DcType DcType { get; set; } = new();
        [JsonPropertyName("success_type")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string SuccessType { get; set; } = "";
    }

    public class DcType
    {
        [JsonPropertyName("index")] public string Index { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("url")] public string Url { get; set; } = "";
    }
}
