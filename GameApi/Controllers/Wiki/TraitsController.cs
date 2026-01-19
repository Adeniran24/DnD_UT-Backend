using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace DndTraits.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TraitsController : ControllerBase
    {
        private static List<Trait> _traits = new();

        public TraitsController()
        {
            InitializeData();
        }

        // GET: api/traits
        [HttpGet]
        public ActionResult<IEnumerable<Trait>> GetTraits()
        {
            return Ok(_traits);
        }

        // GET: api/traits/{index}
        [HttpGet("{index}")]
        public ActionResult<Trait> GetTrait(string index)
        {
            var trait = _traits.FirstOrDefault(t => t.Index.Equals(index, StringComparison.OrdinalIgnoreCase));
            if (trait == null)
            {
                return NotFound();
            }
            return Ok(trait);
        }

        // GET: api/traits/race/{raceIndex}
        [HttpGet("race/{raceIndex}")]
        public ActionResult<IEnumerable<Trait>> GetTraitsByRace(string raceIndex)
        {
            var traits = _traits.Where(t => 
                t.Races.Any(r => r.Index.Equals(raceIndex, StringComparison.OrdinalIgnoreCase)) ||
                t.Subraces.Any(s => s.Index.Equals(raceIndex, StringComparison.OrdinalIgnoreCase))).ToList();
            return Ok(traits);
        }

        // GET: api/traits/search/{name}
        [HttpGet("search/{name}")]
        public ActionResult<IEnumerable<Trait>> SearchTraits(string name)
        {
            var traits = _traits.Where(t => 
                t.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
            return Ok(traits);
        }

        // GET: api/traits/with-proficiencies
        [HttpGet("with-proficiencies")]
        public ActionResult<IEnumerable<Trait>> GetTraitsWithProficiencies()
        {
            var traits = _traits.Where(t => t.Proficiencies != null && t.Proficiencies.Any()).ToList();
            return Ok(traits);
        }

        // GET: api/traits/with-choices
        [HttpGet("with-choices")]
        public ActionResult<IEnumerable<Trait>> GetTraitsWithChoices()
        {
            var traits = _traits.Where(t => t.ProficiencyChoices != null || t.LanguageOptions != null || t.TraitSpecific?.SpellOptions != null).ToList();
            return Ok(traits);
        }

        // Models
        public class Trait
        {
            public string Index { get; set; } = string.Empty;
            public List<RaceReference> Races { get; set; } = new();
            public List<SubraceReference> Subraces { get; set; } = new();
            public string Name { get; set; } = string.Empty;
            public List<string> Description { get; set; } = new();
            public List<ProficiencyReference> Proficiencies { get; set; } = new();
            public ProficiencyChoice? ProficiencyChoices { get; set; }
            public LanguageChoice? LanguageOptions { get; set; }
            public TraitSpecific? TraitSpecific { get; set; }
            public ParentTrait? Parent { get; set; }
            public string Url { get; set; } = string.Empty;
        }

        public class RaceReference
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class SubraceReference
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class ProficiencyReference
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class ProficiencyChoice
        {
            public int Choose { get; set; }
            public string Type { get; set; } = string.Empty;
            public OptionSet? From { get; set; }
        }

        public class LanguageChoice
        {
            public int Choose { get; set; }
            public string Type { get; set; } = string.Empty;
            public OptionSet? From { get; set; }
        }

        public class OptionSet
        {
            public string OptionSetType { get; set; } = string.Empty;
            public List<Option> Options { get; set; } = new();
        }

        public class Option
        {
            public string OptionType { get; set; } = string.Empty;
            public ReferenceItem Item { get; set; } = new();
        }

        public class ReferenceItem
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class TraitSpecific
        {
            public SpellChoice? SpellOptions { get; set; }
            public SubtraitChoice? SubtraitOptions { get; set; }
            public DamageTypeReference? DamageType { get; set; }
            public BreathWeapon? BreathWeapon { get; set; }
        }

        public class SpellChoice
        {
            public int Choose { get; set; }
            public OptionSet? From { get; set; }
            public string Type { get; set; } = string.Empty;
        }

        public class SubtraitChoice
        {
            public int Choose { get; set; }
            public OptionSet? From { get; set; }
            public string Type { get; set; } = string.Empty;
        }

        public class DamageTypeReference
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class BreathWeapon
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public AreaOfEffect AreaOfEffect { get; set; } = new();
            public Usage Usage { get; set; } = new();
            public DifficultyClass Dc { get; set; } = new();
            public List<BreathWeaponDamage> Damage { get; set; } = new();
        }

        public class AreaOfEffect
        {
            public int Size { get; set; }
            public string Type { get; set; } = string.Empty;
        }

        public class Usage
        {
            public string Type { get; set; } = string.Empty;
            public int Times { get; set; }
        }

        public class DifficultyClass
        {
            public AbilityScoreReference DcType { get; set; } = new();
            public string SuccessType { get; set; } = string.Empty;
        }

        public class AbilityScoreReference
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class BreathWeaponDamage
        {
            public DamageTypeReference DamageType { get; set; } = new();
            public Dictionary<string, string> DamageAtCharacterLevel { get; set; } = new();
        }

        public class ParentTrait
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        // Converter methods
        private RaceReference ConvertToRaceReference(dynamic raceData)
        {
            return new RaceReference
            {
                Index = raceData.index,
                Name = raceData.name,
                Url = raceData.url
            };
        }

        private SubraceReference ConvertToSubraceReference(dynamic subraceData)
        {
            return new SubraceReference
            {
                Index = subraceData.index,
                Name = subraceData.name,
                Url = subraceData.url
            };
        }

        private ProficiencyReference ConvertToProficiencyReference(dynamic proficiencyData)
        {
            return new ProficiencyReference
            {
                Index = proficiencyData.index,
                Name = proficiencyData.name,
                Url = proficiencyData.url
            };
        }

        private OptionSet? ConvertToOptionSet(dynamic optionSetData)
        {
            if (optionSetData == null) return null;

            var options = new List<Option>();
            if (optionSetData.options != null)
            {
                foreach (var optionData in optionSetData.options)
                {
                    options.Add(new Option
                    {
                        OptionType = optionData.option_type ?? string.Empty,
                        Item = new ReferenceItem
                        {
                            Index = optionData.item?.index ?? string.Empty,
                            Name = optionData.item?.name ?? string.Empty,
                            Url = optionData.item?.url ?? string.Empty
                        }
                    });
                }
            }

            return new OptionSet
            {
                OptionSetType = optionSetData.option_set_type,
                Options = options
            };
        }

        private ProficiencyChoice? ConvertToProficiencyChoice(dynamic choiceData)
        {
            if (choiceData == null) return null;

            return new ProficiencyChoice
            {
                Choose = choiceData.choose,
                Type = choiceData.type ?? string.Empty,
                From = ConvertToOptionSet(choiceData.@from)
            };
        }

        private LanguageChoice? ConvertToLanguageChoice(dynamic choiceData)
        {
            if (choiceData == null) return null;

            return new LanguageChoice
            {
                Choose = choiceData.choose,
                Type = choiceData.type ?? string.Empty,
                From = ConvertToOptionSet(choiceData.@from)
            };
        }

        private TraitSpecific? ConvertToTraitSpecific(dynamic specificData)
        {
            if (specificData == null) return null;

            return new TraitSpecific
            {
                SpellOptions = ConvertToSpellChoice(specificData.spell_options),
                SubtraitOptions = ConvertToSubtraitChoice(specificData.subtrait_options),
                DamageType = ConvertToDamageTypeReference(specificData.damage_type),
                BreathWeapon = ConvertToBreathWeapon(specificData.breath_weapon)
            };
        }

        private SpellChoice? ConvertToSpellChoice(dynamic spellChoiceData)
        {
            if (spellChoiceData == null) return null;

            return new SpellChoice
            {
                Choose = spellChoiceData.choose,
                From = ConvertToOptionSet(spellChoiceData.@from),
                Type = spellChoiceData.type ?? string.Empty
            };
        }

        private SubtraitChoice? ConvertToSubtraitChoice(dynamic subtraitChoiceData)
        {
            if (subtraitChoiceData == null) return null;

            return new SubtraitChoice
            {
                Choose = subtraitChoiceData.choose,
                From = ConvertToOptionSet(subtraitChoiceData.@from),
                Type = subtraitChoiceData.type ?? string.Empty
            };
        }

        private DamageTypeReference? ConvertToDamageTypeReference(dynamic damageTypeData)
        {
            if (damageTypeData == null) return null;

            return new DamageTypeReference
            {
                Index = damageTypeData.index ?? string.Empty,
                Name = damageTypeData.name ?? string.Empty,
                Url = damageTypeData.url ?? string.Empty
            };
        }

        private BreathWeapon? ConvertToBreathWeapon(dynamic breathWeaponData)
        {
            if (breathWeaponData == null) return null;

            var damage = new List<BreathWeaponDamage>();
            if (breathWeaponData.damage != null)
            {
                foreach (var damageData in breathWeaponData.damage)
                {
                    damage.Add(new BreathWeaponDamage
                    {
                        DamageType = ConvertToDamageTypeReference(damageData.damage_type) ?? new DamageTypeReference(),
                        DamageAtCharacterLevel = ((Dictionary<string, object>)damageData.damage_at_character_level)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
                    });
                }
            }

            return new BreathWeapon
            {
                Name = breathWeaponData.name ?? string.Empty,
                Description = breathWeaponData.desc ?? string.Empty,
                AreaOfEffect = new AreaOfEffect
                {
                    Size = breathWeaponData.area_of_effect?.size ?? 0,
                    Type = breathWeaponData.area_of_effect?.type ?? string.Empty
                },
                Usage = new Usage
                {
                    Type = breathWeaponData.usage?.type ?? string.Empty,
                    Times = breathWeaponData.usage?.times ?? 0
                },
                Dc = new DifficultyClass
                {
                    DcType = new AbilityScoreReference
                    {
                        Index = breathWeaponData.dc?.dc_type?.index ?? string.Empty,
                        Name = breathWeaponData.dc?.dc_type?.name ?? string.Empty,
                        Url = breathWeaponData.dc?.dc_type?.url ?? string.Empty
                    },
                    SuccessType = breathWeaponData.dc?.success_type ?? string.Empty
                },
                Damage = damage
            };
        }

        private ParentTrait? ConvertToParentTrait(dynamic parentData)
        {
            if (parentData == null) return null;

            return new ParentTrait
            {
                Index = parentData.index ?? string.Empty,
                Name = parentData.name ?? string.Empty,
                Url = parentData.url ?? string.Empty
            };
        }

        private void InitializeData()
        {
            // Initialize with sample data - in a real application, this would come from a database
            _traits = new List<Trait>
            {
                new Trait
                {
                    Index = "darkvision",
                    Name = "Darkvision",
                    Description = new List<string>
                    {
                        "You have superior vision in dark and dim conditions. You can see in dim light within 60 feet of you as if it were bright light, and in darkness as if it were dim light. You cannot discern color in darkness, only shades of gray."
                    },
                    Races = new List<RaceReference>
                    {
                        new RaceReference { Index = "dwarf", Name = "Dwarf", Url = "/api/2014/races/dwarf" },
                        new RaceReference { Index = "elf", Name = "Elf", Url = "/api/2014/races/elf" },
                        new RaceReference { Index = "gnome", Name = "Gnome", Url = "/api/2014/races/gnome" },
                        new RaceReference { Index = "half-elf", Name = "Half-Elf", Url = "/api/2014/races/half-elf" },
                        new RaceReference { Index = "half-orc", Name = "Half-Orc", Url = "/api/2014/races/half-orc" },
                        new RaceReference { Index = "tiefling", Name = "Tiefling", Url = "/api/2014/races/tiefling" }
                    },
                    Subraces = new List<SubraceReference>(),
                    Proficiencies = new List<ProficiencyReference>(),
                    Url = "/api/2014/traits/darkvision"
                },
                new Trait
                {
                    Index = "dwarven-resilience",
                    Name = "Dwarven Resilience",
                    Description = new List<string>
                    {
                        "You have advantage on saving throws against poison, and you have resistance against poison damage."
                    },
                    Races = new List<RaceReference>
                    {
                        new RaceReference { Index = "dwarf", Name = "Dwarf", Url = "/api/2014/races/dwarf" }
                    },
                    Subraces = new List<SubraceReference>(),
                    Proficiencies = new List<ProficiencyReference>(),
                    Url = "/api/2014/traits/dwarven-resilience"
                }
                // Add more traits as needed...
            };
        }

        // Main converter method for JSON data
        private Trait ConvertJsonToTrait(dynamic jsonData)
        {
            var races = new List<RaceReference>();
            if (jsonData.races != null)
            {
                foreach (var raceData in jsonData.races)
                {
                    races.Add(ConvertToRaceReference(raceData));
                }
            }

            var subraces = new List<SubraceReference>();
            if (jsonData.subraces != null)
            {
                foreach (var subraceData in jsonData.subraces)
                {
                    subraces.Add(ConvertToSubraceReference(subraceData));
                }
            }

            var proficiencies = new List<ProficiencyReference>();
            if (jsonData.proficiencies != null)
            {
                foreach (var proficiencyData in jsonData.proficiencies)
                {
                    proficiencies.Add(ConvertToProficiencyReference(proficiencyData));
                }
            }

            return new Trait
            {
                Index = jsonData.index,
                Races = races,
                Subraces = subraces,
                Name = jsonData.name,
                Description = ((List<object>)jsonData.desc).Cast<string>().ToList(),
                Proficiencies = proficiencies,
                ProficiencyChoices = ConvertToProficiencyChoice(jsonData.proficiency_choices),
                LanguageOptions = ConvertToLanguageChoice(jsonData.language_options),
                TraitSpecific = ConvertToTraitSpecific(jsonData.trait_specific),
                Parent = ConvertToParentTrait(jsonData.parent),
                Url = jsonData.url
            };
        }

        // Helper methods for specific queries
        [HttpGet("{index}/races")]
        public ActionResult<List<object>> GetTraitRaces(string index)
        {
            var trait = _traits.FirstOrDefault(t => t.Index.Equals(index, StringComparison.OrdinalIgnoreCase));
            if (trait == null)
            {
                return NotFound();
            }

            var races = new List<object>();
            races.AddRange(trait.Races);
            races.AddRange(trait.Subraces);
            
            return Ok(races);
        }

        [HttpGet("damage-types/{damageType}")]
        public ActionResult<IEnumerable<Trait>> GetTraitsByDamageType(string damageType)
        {
            var traits = _traits.Where(t => 
                t.TraitSpecific?.DamageType?.Index?.Equals(damageType, StringComparison.OrdinalIgnoreCase) == true).ToList();
            return Ok(traits);
        }
    }
}
