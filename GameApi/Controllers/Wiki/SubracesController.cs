using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace DndSubraces.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubracesController : ControllerBase
    {
        private static List<Subrace> _subraces = new();

        public SubracesController()
        {
            InitializeData();
        }

        // GET: api/subraces
        [HttpGet]
        public ActionResult<IEnumerable<Subrace>> GetSubraces()
        {
            return Ok(_subraces);
        }

        // GET: api/subraces/{index}
        [HttpGet("{index}")]
        public ActionResult<Subrace> GetSubrace(string index)
        {
            var subrace = _subraces.FirstOrDefault(s => s.Index.Equals(index, StringComparison.OrdinalIgnoreCase));
            if (subrace == null)
            {
                return NotFound();
            }
            return Ok(subrace);
        }

        // GET: api/subraces/race/{raceName}
        [HttpGet("race/{raceName}")]
        public ActionResult<IEnumerable<Subrace>> GetSubracesByRace(string raceName)
        {
            var subraces = _subraces.Where(s => 
                s.Race.Name.Equals(raceName, StringComparison.OrdinalIgnoreCase)).ToList();
            return Ok(subraces);
        }

        // GET: api/subraces/search/{name}
        [HttpGet("search/{name}")]
        public ActionResult<IEnumerable<Subrace>> SearchSubraces(string name)
        {
            var subraces = _subraces.Where(s => 
                s.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
            return Ok(subraces);
        }

        // Models
        public class Subrace
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public RaceInfo Race { get; set; } = new();
            public string Description { get; set; } = string.Empty;
            public List<AbilityBonus> AbilityBonuses { get; set; } = new();
            public List<RacialTrait> RacialTraits { get; set; } = new();
            public string Url { get; set; } = string.Empty;
        }

        public class RaceInfo
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class AbilityBonus
        {
            public AbilityScore AbilityScore { get; set; } = new();
            public int Bonus { get; set; }
        }

        public class AbilityScore
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class RacialTrait
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        // Converter methods
        private RaceInfo ConvertToRaceInfo(dynamic raceData)
        {
            return new RaceInfo
            {
                Index = raceData.index,
                Name = raceData.name,
                Url = raceData.url
            };
        }

        private List<AbilityBonus> ConvertToAbilityBonuses(List<dynamic> abilityBonusesData)
        {
            var abilityBonuses = new List<AbilityBonus>();
            foreach (var bonusData in abilityBonusesData)
            {
                abilityBonuses.Add(new AbilityBonus
                {
                    AbilityScore = new AbilityScore
                    {
                        Index = bonusData.ability_score.index,
                        Name = bonusData.ability_score.name,
                        Url = bonusData.ability_score.url
                    },
                    Bonus = bonusData.bonus
                });
            }
            return abilityBonuses;
        }

        private List<RacialTrait> ConvertToRacialTraits(List<dynamic> traitsData)
        {
            var traits = new List<RacialTrait>();
            foreach (var traitData in traitsData)
            {
                traits.Add(new RacialTrait
                {
                    Index = traitData.index,
                    Name = traitData.name,
                    Url = traitData.url
                });
            }
            return traits;
        }

        private void InitializeData()
        {
            _subraces = new List<Subrace>
            {
                new Subrace
                {
                    Index = "hill-dwarf",
                    Name = "Hill Dwarf",
                    Race = new RaceInfo { Index = "dwarf", Name = "Dwarf", Url = "/api/2014/races/dwarf" },
                    Description = "As a hill dwarf, you have keen senses, deep intuition, and remarkable resilience.",
                    AbilityBonuses = new List<AbilityBonus>
                    {
                        new AbilityBonus
                        {
                            AbilityScore = new AbilityScore { Index = "wis", Name = "WIS", Url = "/api/2014/ability-scores/wis" },
                            Bonus = 1
                        }
                    },
                    RacialTraits = new List<RacialTrait>
                    {
                        new RacialTrait { Index = "dwarven-toughness", Name = "Dwarven Toughness", Url = "/api/2014/traits/dwarven-toughness" }
                    },
                    Url = "/api/2014/subraces/hill-dwarf"
                },
                new Subrace
                {
                    Index = "high-elf",
                    Name = "High Elf",
                    Race = new RaceInfo { Index = "elf", Name = "Elf", Url = "/api/2014/races/elf" },
                    Description = "As a high elf, you have a keen mind and a mastery of at least the basics of magic. In many fantasy gaming worlds, there are two kinds of high elves. One type is haughty and reclusive, believing themselves to be superior to non-elves and even other elves. The other type is more common and more friendly, and often encountered among humans and other races.",
                    AbilityBonuses = new List<AbilityBonus>
                    {
                        new AbilityBonus
                        {
                            AbilityScore = new AbilityScore { Index = "int", Name = "INT", Url = "/api/2014/ability-scores/int" },
                            Bonus = 1
                        }
                    },
                    RacialTraits = new List<RacialTrait>
                    {
                        new RacialTrait { Index = "elf-weapon-training", Name = "Elf Weapon Training", Url = "/api/2014/traits/elf-weapon-training" },
                        new RacialTrait { Index = "high-elf-cantrip", Name = "High Elf Cantrip", Url = "/api/2014/traits/high-elf-cantrip" },
                        new RacialTrait { Index = "extra-language", Name = "Extra Language", Url = "/api/2014/traits/extra-language" }
                    },
                    Url = "/api/2014/subraces/high-elf"
                },
                new Subrace
                {
                    Index = "lightfoot-halfling",
                    Name = "Lightfoot Halfling",
                    Race = new RaceInfo { Index = "halfling", Name = "Halfling", Url = "/api/2014/races/halfling" },
                    Description = "As a lightfoot halfling, you can easily hide from notice, even using other people as cover. You're inclined to be affable and get along well with others. Lightfoots are more prone to wanderlust than other halflings, and often dwell alongside other races or take up a nomadic life.",
                    AbilityBonuses = new List<AbilityBonus>
                    {
                        new AbilityBonus
                        {
                            AbilityScore = new AbilityScore { Index = "cha", Name = "CHA", Url = "/api/2014/ability-scores/cha" },
                            Bonus = 1
                        }
                    },
                    RacialTraits = new List<RacialTrait>
                    {
                        new RacialTrait { Index = "naturally-stealthy", Name = "Naturally Stealthy", Url = "/api/2014/traits/naturally-stealthy" }
                    },
                    Url = "/api/2014/subraces/lightfoot-halfling"
                },
                new Subrace
                {
                    Index = "rock-gnome",
                    Name = "Rock Gnome",
                    Race = new RaceInfo { Index = "gnome", Name = "Gnome", Url = "/api/2014/races/gnome" },
                    Description = "As a rock gnome, you have a natural inventiveness and hardiness beyond that of other gnomes.",
                    AbilityBonuses = new List<AbilityBonus>
                    {
                        new AbilityBonus
                        {
                            AbilityScore = new AbilityScore { Index = "con", Name = "CON", Url = "/api/2014/ability-scores/con" },
                            Bonus = 1
                        }
                    },
                    RacialTraits = new List<RacialTrait>
                    {
                        new RacialTrait { Index = "artificers-lore", Name = "Artificer's Lore", Url = "/api/2014/traits/artificers-lore" },
                        new RacialTrait { Index = "tinker", Name = "Tinker", Url = "/api/2014/traits/tinker" }
                    },
                    Url = "/api/2014/subraces/rock-gnome"
                }
            };
        }

        // Main converter method for JSON data
        private Subrace ConvertJsonToSubrace(dynamic jsonData)
        {
            return new Subrace
            {
                Index = jsonData.index,
                Name = jsonData.name,
                Race = ConvertToRaceInfo(jsonData.race),
                Description = jsonData.desc,
                AbilityBonuses = ConvertToAbilityBonuses(jsonData.ability_bonuses != null ? 
                    ((List<object>)jsonData.ability_bonuses).Cast<dynamic>().ToList() : new List<dynamic>()),
                RacialTraits = ConvertToRacialTraits(jsonData.racial_traits != null ? 
                    ((List<object>)jsonData.racial_traits).Cast<dynamic>().ToList() : new List<dynamic>()),
                Url = jsonData.url
            };
        }

        // Helper method to get total ability bonuses for a subrace
        [HttpGet("{index}/total-bonuses")]
        public ActionResult<Dictionary<string, int>> GetTotalAbilityBonuses(string index)
        {
            var subrace = _subraces.FirstOrDefault(s => s.Index.Equals(index, StringComparison.OrdinalIgnoreCase));
            if (subrace == null)
            {
                return NotFound();
            }

            var totalBonuses = new Dictionary<string, int>();
            foreach (var bonus in subrace.AbilityBonuses)
            {
                totalBonuses[bonus.AbilityScore.Name] = bonus.Bonus;
            }

            return Ok(totalBonuses);
        }

        // Helper method to get racial traits for a subrace
        [HttpGet("{index}/traits")]
        public ActionResult<List<RacialTrait>> GetSubraceTraits(string index)
        {
            var subrace = _subraces.FirstOrDefault(s => s.Index.Equals(index, StringComparison.OrdinalIgnoreCase));
            if (subrace == null)
            {
                return NotFound();
            }

            return Ok(subrace.RacialTraits);
        }
    }
}
