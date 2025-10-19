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
        private static List<Trait> _traits;

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
            public string Index { get; set; }
            public List<RaceReference> Races { get; set; }
            public List<SubraceReference> Subraces { get; set; }
            public string Name { get; set; }
            public List<string> Description { get; set; }
            public List<ProficiencyReference> Proficiencies { get; set; }
            public ProficiencyChoice ProficiencyChoices { get; set; }
            public LanguageChoice LanguageOptions { get; set; }
            public TraitSpecific TraitSpecific { get; set; }
            public ParentTrait Parent { get; set; }
            public string Url { get; set; }
        }

        public class RaceReference
        {
            public string Index { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
        }

        public class SubraceReference
        {
            public string Index { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
        }

        public class ProficiencyReference
        {
            public string Index { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
        }

        public class ProficiencyChoice
        {
            public int Choose { get; set; }
            public string Type { get; set; }
            public OptionSet From { get; set; }
        }

        public class LanguageChoice
        {
            public int Choose { get; set; }
            public string Type { get; set; }
            public OptionSet From { get; set; }
        }

        public class OptionSet
        {
            public string OptionSetType { get; set; }
            public List<Option> Options { get; set; }
        }

        public class Option
        {
            public string OptionType { get; set; }
            public ReferenceItem Item { get; set; }
        }

        public class ReferenceItem
        {
            public string Index { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
        }

        public class TraitSpecific
        {
            public SpellChoice SpellOptions { get; set; }
            public SubtraitChoice SubtraitOptions { get; set; }
            public DamageTypeReference DamageType { get; set; }
            public BreathWeapon BreathWeapon { get; set; }
        }

        public class SpellChoice
        {
            public int Choose { get; set; }
            public OptionSet From { get; set; }
            public string Type { get; set; }
        }

        public class SubtraitChoice
        {
            public int Choose { get; set; }
            public OptionSet From { get; set; }
            public string Type { get; set; }
        }

        public class DamageTypeReference
        {
            public string Index { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
        }

        public class BreathWeapon
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public AreaOfEffect AreaOfEffect { get; set; }
            public Usage Usage { get; set; }
            public DifficultyClass Dc { get; set; }
            public List<BreathWeaponDamage> Damage { get; set; }
        }

        public class AreaOfEffect
        {
            public int Size { get; set; }
            public string Type { get; set; }
        }

        public class Usage
        {
            public string Type { get; set; }
            public int Times { get; set; }
        }

        public class DifficultyClass
        {
            public AbilityScoreReference DcType { get; set; }
            public string SuccessType { get; set; }
        }

        public class AbilityScoreReference
        {
            public string Index { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
        }

        public class BreathWeaponDamage
        {
            public DamageTypeReference DamageType { get; set; }
            public Dictionary<string, string> DamageAtCharacterLevel { get; set; }
        }

        public class ParentTrait
        {
            public string Index { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
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

        private OptionSet ConvertToOptionSet(dynamic optionSetData)
        {
            if (optionSetData == null) return null;

            var options = new List<Option>();
            foreach (var optionData in optionSetData.options)
            {
                options.Add(new Option
                {
                    OptionType = optionData.option_type,
                    Item = new ReferenceItem
                    {
                        Index = optionData.item.index,
                        Name = optionData.item.name,
                        Url = optionData.item.url
                    }
                });
            }

            return new OptionSet
            {
                OptionSetType = optionSetData.option_set_type,
                Options = options
            };
        }

        private ProficiencyChoice ConvertToProficiencyChoice(dynamic choiceData)
        {
            if (choiceData == null) return null;

            return new ProficiencyChoice
            {
                Choose = choiceData.choose,
                Type = choiceData.type,
                From = ConvertToOptionSet(choiceData.@from)
            };
        }

        private LanguageChoice ConvertToLanguageChoice(dynamic choiceData)
        {
            if (choiceData == null) return null;

            return new LanguageChoice
            {
                Choose = choiceData.choose,
                Type = choiceData.type,
                From = ConvertToOptionSet(choiceData.@from)
            };
        }

        private TraitSpecific ConvertToTraitSpecific(dynamic specificData)
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

        private SpellChoice ConvertToSpellChoice(dynamic spellChoiceData)
        {
            if (spellChoiceData == null) return null;

            return new SpellChoice
            {
                Choose = spellChoiceData.choose,
                From = ConvertToOptionSet(spellChoiceData.@from),
                Type = spellChoiceData.type
            };
        }

        private SubtraitChoice ConvertToSubtraitChoice(dynamic subtraitChoiceData)
        {
            if (subtraitChoiceData == null) return null;

            return new SubtraitChoice
            {
                Choose = subtraitChoiceData.choose,
                From = ConvertToOptionSet(subtraitChoiceData.@from),
                Type = subtraitChoiceData.type
            };
        }

        private DamageTypeReference ConvertToDamageTypeReference(dynamic damageTypeData)
        {
            if (damageTypeData == null) return null;

            return new DamageTypeReference
            {
                Index = damageTypeData.index,
                Name = damageTypeData.name,
                Url = damageTypeData.url
            };
        }

        private BreathWeapon ConvertToBreathWeapon(dynamic breathWeaponData)
        {
            if (breathWeaponData == null) return null;

            var damage = new List<BreathWeaponDamage>();
            if (breathWeaponData.damage != null)
            {
                foreach (var damageData in breathWeaponData.damage)
                {
                    damage.Add(new BreathWeaponDamage
                    {
                        DamageType = ConvertToDamageTypeReference(damageData.damage_type),
                        DamageAtCharacterLevel = ((Dictionary<string, object>)damageData.damage_at_character_level)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                    });
                }
            }

            return new BreathWeapon
            {
                Name = breathWeaponData.name,
                Description = breathWeaponData.desc,
                AreaOfEffect = new AreaOfEffect
                {
                    Size = breathWeaponData.area_of_effect?.size ?? 0,
                    Type = breathWeaponData.area_of_effect?.type
                },
                Usage = new Usage
                {
                    Type = breathWeaponData.usage?.type,
                    Times = breathWeaponData.usage?.times ?? 0
                },
                Dc = new DifficultyClass
                {
                    DcType = new AbilityScoreReference
                    {
                        Index = breathWeaponData.dc?.dc_type?.index,
                        Name = breathWeaponData.dc?.dc_type?.name,
                        Url = breathWeaponData.dc?.dc_type?.url
                    },
                    SuccessType = breathWeaponData.dc?.success_type
                },
                Damage = damage
            };
        }

        private ParentTrait ConvertToParentTrait(dynamic parentData)
        {
            if (parentData == null) return null;

            return new ParentTrait
            {
                Index = parentData.index,
                Name = parentData.name,
                Url = parentData.url
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