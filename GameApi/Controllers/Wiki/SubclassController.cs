using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace DndSubclasses.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubclassesController : ControllerBase
    {
        private static List<Subclass> _subclasses = new();

        public SubclassesController()
        {
            InitializeData();
        }

        // GET: api/subclasses
        [HttpGet]
        public ActionResult<IEnumerable<Subclass>> GetSubclasses()
        {
            return Ok(_subclasses);
        }

        // GET: api/subclasses/{index}
        [HttpGet("{index}")]
        public ActionResult<Subclass> GetSubclass(string index)
        {
            var subclass = _subclasses.FirstOrDefault(s => s.Index.Equals(index, StringComparison.OrdinalIgnoreCase));
            if (subclass == null)
            {
                return NotFound();
            }
            return Ok(subclass);
        }

        // GET: api/subclasses/class/{className}
        [HttpGet("class/{className}")]
        public ActionResult<IEnumerable<Subclass>> GetSubclassesByClass(string className)
        {
            var subclasses = _subclasses.Where(s => 
                s.Class.Name.Equals(className, StringComparison.OrdinalIgnoreCase)).ToList();
            return Ok(subclasses);
        }

        // Models
        public class Subclass
        {
            public string Index { get; set; } = string.Empty;
            public ClassInfo Class { get; set; } = new();
            public string Name { get; set; } = string.Empty;
            public string SubclassFlavor { get; set; } = string.Empty;
            public List<string> Description { get; set; } = new();
            public string SubclassLevels { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public List<SubclassSpell> Spells { get; set; } = new();
        }

        public class ClassInfo
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class SubclassSpell
        {
            public List<Prerequisite> Prerequisites { get; set; } = new();
            public SpellInfo Spell { get; set; } = new();
        }

        public class Prerequisite
        {
            public string Index { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class SpellInfo
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        // Converter methods
        private ClassInfo ConvertToClassInfo(dynamic classData)
        {
            return new ClassInfo
            {
                Index = classData.index,
                Name = classData.name,
                Url = classData.url
            };
        }

        private List<SubclassSpell> ConvertToSpells(List<dynamic>? spellsData)
        {
            if (spellsData == null) return new List<SubclassSpell>();

            var spells = new List<SubclassSpell>();
            foreach (var spellData in spellsData)
            {
                var prerequisites = new List<Prerequisite>();
                foreach (var prereqData in spellData.prerequisites)
                {
                    prerequisites.Add(new Prerequisite
                    {
                        Index = prereqData.index,
                        Type = prereqData.type,
                        Name = prereqData.name,
                        Url = prereqData.url
                    });
                }

                spells.Add(new SubclassSpell
                {
                    Prerequisites = prerequisites,
                    Spell = new SpellInfo
                    {
                        Index = spellData.spell.index,
                        Name = spellData.spell.name,
                        Url = spellData.spell.url
                    }
                });
            }
            return spells;
        }

        private void InitializeData()
        {
            // This would typically come from a database or external API
            // For this example, we'll create the data manually based on the JSON provided
            
            _subclasses = new List<Subclass>
            {
                new Subclass
                {
                    Index = "berserker",
                    Class = new ClassInfo { Index = "barbarian", Name = "Barbarian", Url = "/api/2014/classes/barbarian" },
                    Name = "Berserker",
                    SubclassFlavor = "Primal Path",
                    Description = new List<string> { "For some barbarians, rage is a means to an end--that end being violence. The Path of the Berserker is a path of untrammeled fury, slick with blood. As you enter the berserker's rage, you thrill in the chaos of battle, heedless of your own health or well-being." },
                    SubclassLevels = "/api/2014/subclasses/berserker/levels",
                    Url = "/api/2014/subclasses/berserker",
                    Spells = new List<SubclassSpell>()
                },
                new Subclass
                {
                    Index = "lore",
                    Class = new ClassInfo { Index = "bard", Name = "Bard", Url = "/api/2014/classes/bard" },
                    Name = "Lore",
                    SubclassFlavor = "Bard College",
                    Description = new List<string> { "Bards of the College of Lore know something about most things, collecting bits of knowledge from sources as diverse as scholarly tomes and peasant tales. Whether singing folk ballads in taverns or elaborate compositions in royal courts, these bards use their gifts to hold audiences spellbound. When the applause dies down, the audience members might find themselves questioning everything they held to be true, from their faith in the priesthood of the local temple to their loyalty to the king. The loyalty of these bards lies in the pursuit of beauty and truth, not in fealty to a monarch or following the tenets of a deity. A noble who keeps such a bard as a herald or advisor knows that the bard would rather be honest than politic. The college's members gather in libraries and sometimes in actual colleges, complete with classrooms and dormitories, to share their lore with one another. They also meet at festivals or affairs of state, where they can expose corruption, unravel lies, and poke fun at self-important figures of authority." },
                    SubclassLevels = "/api/2014/subclasses/lore/levels",
                    Url = "/api/2014/subclasses/lore",
                    Spells = new List<SubclassSpell>()
                },
                new Subclass
                {
                    Index = "life",
                    Class = new ClassInfo { Index = "cleric", Name = "Cleric", Url = "/api/2014/classes/cleric" },
                    Name = "Life",
                    SubclassFlavor = "Divine Domain",
                    Description = new List<string> { "The Life domain focuses on the vibrant positive energy--one of the fundamental forces of the universe--that sustains all life. The gods of life promote vitality and health through healing the sick and wounded, caring for those in need, and driving away the forces of death and undeath. Almost any non-evil deity can claim influence over this domain, particularly agricultural deities, sun gods, gods of healing or endurance, and gods of home and community." },
                    SubclassLevels = "/api/2014/subclasses/life/levels",
                    Url = "/api/2014/subclasses/life",
                    Spells = new List<SubclassSpell>
                    {
                        new SubclassSpell
                        {
                            Prerequisites = new List<Prerequisite> { new Prerequisite { Index = "cleric-1", Type = "level", Name = "Cleric 1", Url = "/api/2014/classes/cleric/levels/1" } },
                            Spell = new SpellInfo { Index = "bless", Name = "Bless", Url = "/api/2014/spells/bless" }
                        },
                        new SubclassSpell
                        {
                            Prerequisites = new List<Prerequisite> { new Prerequisite { Index = "cleric-1", Type = "level", Name = "Cleric 1", Url = "/api/2014/classes/cleric/levels/1" } },
                            Spell = new SpellInfo { Index = "cure-wounds", Name = "Cure Wounds", Url = "/api/2014/spells/cure-wounds" }
                        }
                        // Add remaining spells similarly...
                    }
                }
                // Add remaining subclasses similarly...
            };
        }

        // Helper method to convert JSON data to our models (if we were receiving external JSON)
        private Subclass ConvertJsonToSubclass(dynamic jsonData)
        {
            return new Subclass
            {
                Index = jsonData.index,
                Class = ConvertToClassInfo(jsonData.@class),
                Name = jsonData.name,
                SubclassFlavor = jsonData.subclass_flavor,
                Description = ((List<object>)jsonData.desc).Cast<string>().ToList(),
                SubclassLevels = jsonData.subclass_levels,
                Url = jsonData.url,
                Spells = ConvertToSpells(jsonData.spells != null ? ((List<object>)jsonData.spells).Cast<dynamic>().ToList() : null)
            };
        }
    }
}
