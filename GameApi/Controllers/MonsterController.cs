using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GameApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonstersController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _jsonFile;

        public MonstersController(IWebHostEnvironment env)
        {
            _env = env;
            _jsonFile = Path.Combine(_env.ContentRootPath, "Database", "2014", "5e-SRD-Monsters.json");
        }

        [HttpGet("{index}")]
        public IActionResult GetMonster(string index)
        {
            if (!System.IO.File.Exists(_jsonFile))
                return NotFound("Monsters file not found.");

            var json = System.IO.File.ReadAllText(_jsonFile);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var monsters = JsonSerializer.Deserialize<List<Monster>>(json, options);

            var monster = monsters?.FirstOrDefault(m => m.Index.ToLower() == index.ToLower());
            if (monster == null)
                return NotFound();

            return Ok(monster);
        }

        // -------------------------------
        // MODELEK A CONTROLLERBEN
        // -------------------------------

        public class Monster
        {
            public string Index { get; set; } = "";
            public string Name { get; set; } = "";
            public string Desc { get; set; } = "";
            public string Size { get; set; } = "";
            public string Type { get; set; } = "";
            public string Subtype { get; set; } = "";
            public string Alignment { get; set; } = "";
            public List<ArmorClass> ArmorClass { get; set; } = new();
            public int? HitPoints { get; set; }
            public string HitDice { get; set; } = "";
            public string HitPointsRoll { get; set; } = "";
            public Speed Speed { get; set; } = new();
            public int Strength { get; set; }
            public int Dexterity { get; set; }
            public int Constitution { get; set; }
            public int Intelligence { get; set; }
            public int Wisdom { get; set; }
            public int Charisma { get; set; }
            public List<Proficiency> Proficiencies { get; set; } = new();
            public List<string> DamageVulnerabilities { get; set; } = new();
            public List<string> DamageResistances { get; set; } = new();
            public List<string> DamageImmunities { get; set; } = new();
            public List<string> ConditionImmunities { get; set; } = new();
            public Senses Senses { get; set; } = new();
            public string Languages { get; set; } = "";
            public double? ChallengeRating { get; set; }
            public int? ProficiencyBonus { get; set; }
            public int? Xp { get; set; }
            public List<SpecialAbility> SpecialAbilities { get; set; } = new();
            public List<Action> Actions { get; set; } = new();
            public List<LegendaryAction> LegendaryActions { get; set; } = new();
            public List<Reaction> Reactions { get; set; } = new();
            public List<string> Forms { get; set; } = new();
            public string Image { get; set; } = "";
            public string Url { get; set; } = "";
            public string UpdatedAt { get; set; } = "";
        }

        public class ArmorClass
        {
            public string Type { get; set; } = "";
            public int Value { get; set; }
        }

        public class Speed
        {
            public string Walk { get; set; } = "";
            public string Fly { get; set; } = "";
            public string Swim { get; set; } = "";
            public string Climb { get; set; } = "";
            public string Burrow { get; set; } = "";
            public bool? Hover { get; set; }
        }

        public class Proficiency
        {
            public int Value { get; set; }
            public ProficiencyInfo ProficiencyInfo { get; set; } = new();
        }

        public class ProficiencyInfo
        {
            public string Index { get; set; } = "";
            public string Name { get; set; } = "";
            public string Url { get; set; } = "";
        }

        public class Senses
        {
            public string Darkvision { get; set; } = "";
            public string Blindsight { get; set; } = "";
            public string Tremorsense { get; set; } = "";
            public string Truesight { get; set; } = "";
            public int? PassivePerception { get; set; }
        }

        public class SpecialAbility
        {
            public string Name { get; set; } = "";
            public string Desc { get; set; } = "";
            public DC? Dc { get; set; }
        }

        public class DC
        {
            public ProficiencyInfo DcType { get; set; } = new();
            public int? DcValue { get; set; }
            public string SuccessType { get; set; } = "";
        }

        public class Action
        {
            public string Name { get; set; } = "";
            public string MultiattackType { get; set; } = "";
            public string Desc { get; set; } = "";
            public int? AttackBonus { get; set; }
            public DC? Dc { get; set; }
            public List<Damage> Damage { get; set; } = new();
            public List<SubAction> Actions { get; set; } = new();
            public Usage? Usage { get; set; }
        }

        public class SubAction
        {
            public string ActionName { get; set; } = "";
            public string Count { get; set; } = ""; // <-- string a JSON miatt
            public string Type { get; set; } = "";
        }

        public class Damage
        {
            public ProficiencyInfo DamageType { get; set; } = new();
            public string DamageDice { get; set; } = "";
        }

        public class Usage
        {
            public string Type { get; set; } = "";
            public string Dice { get; set; } = "";
            public int? MinValue { get; set; }
            public int? Times { get; set; }
            public List<string> RestTypes { get; set; } = new();
        }

        public class LegendaryAction
        {
            public string Name { get; set; } = "";
            public string Desc { get; set; } = "";
            public List<Damage> Damage { get; set; } = new();
        }

        public class Reaction
        {
            public string Name { get; set; } = "";
            public string Desc { get; set; } = "";
        }
    }
}
