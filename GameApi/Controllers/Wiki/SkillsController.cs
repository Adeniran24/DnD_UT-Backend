// Controllers/SkillsController.cs
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DnDAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkillsController : ControllerBase
    {
        private readonly ILogger<SkillsController> _logger;
        private readonly List<Skill> _skills;

        public SkillsController(ILogger<SkillsController> logger)
        {
            _logger = logger;
            _skills = LoadSkillsFromFile();
        }

        // =========================
        // === Models
        // =========================
        public class Skill
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public List<string> Desc { get; set; } = new();
            [JsonPropertyName("ability_score")]
            public SkillAbilityScore Ability_Score { get; set; } = new();
            public string Url { get; set; } = string.Empty;
        }

        public class SkillAbilityScore
        {
            public string Index { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        // =========================
        // === Utilities
        // =========================
        private static class SkillUtils
        {
            public static string GetFullDescription(Skill skill) => string.Join(" ", skill.Desc);

            public static List<string> ExtractExamples(Skill skill)
            {
                var examples = new List<string>();
                foreach (var desc in skill.Desc)
                {
                    var sentences = desc.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var sentence in sentences)
                    {
                        var trimmed = sentence.Trim();
                        if (trimmed.Contains("such as", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.Contains("example", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.Contains("including", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.Contains("typical", StringComparison.OrdinalIgnoreCase))
                        {
                            examples.Add(trimmed + ".");
                        }
                    }
                }
                return examples;
            }

            public static Dictionary<string, int> CountSkillsByAbility(List<Skill> skills)
            {
                return skills
                    .GroupBy(s => s.Ability_Score.Name)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        }

        // =========================
        // === Load JSON
        // =========================
        private List<Skill> LoadSkillsFromFile()
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "2014", "5e-SRD-Skills.json");
                if (!System.IO.File.Exists(filePath)) return new List<Skill>();

                var jsonData = System.IO.File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                return JsonSerializer.Deserialize<List<Skill>>(jsonData, options) ?? new List<Skill>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading skills from file");
                return new List<Skill>();
            }
        }

        // =========================
        // === Endpoints
        // =========================

        [HttpGet]
        public ActionResult<IEnumerable<Skill>> GetAllSkills() => Ok(_skills);

        [HttpGet("{index}")]
        public ActionResult<Skill> GetSkill(string index)
        {
            var skill = _skills.FirstOrDefault(s => s.Index.Equals(index, StringComparison.OrdinalIgnoreCase));
            return skill == null ? NotFound($"Skill '{index}' not found.") : Ok(skill);
        }

        [HttpGet("ability/{abilityIndex}")]
        public ActionResult<IEnumerable<Skill>> GetSkillsByAbility(string abilityIndex)
        {
            var skills = _skills.Where(s => s.Ability_Score.Index.Equals(abilityIndex, StringComparison.OrdinalIgnoreCase));
            return Ok(skills);
        }

        [HttpGet("search")]
        public ActionResult<IEnumerable<Skill>> SearchSkills([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Name parameter is required.");
            var skills = _skills.Where(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            return Ok(skills);
        }

        [HttpGet("search/description")]
        public ActionResult<IEnumerable<Skill>> SearchSkillsByDescription([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return BadRequest("Keyword parameter is required.");
            var skills = _skills.Where(s => s.Desc.Any(d => d.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            return Ok(skills);
        }

        [HttpGet("ability-scores")]
        public ActionResult<IEnumerable<string>> GetSkillAbilityScores()
        {
            var abilityScores = _skills.Select(s => s.Ability_Score.Name).Distinct().OrderBy(a => a);
            return Ok(abilityScores);
        }

        [HttpGet("grouped-by-ability")]
        public ActionResult<Dictionary<string, List<Skill>>> GetSkillsGroupedByAbility()
        {
            var groupedSkills = _skills.GroupBy(s => s.Ability_Score.Name)
                                       .ToDictionary(g => g.Key, g => g.ToList());
            return Ok(groupedSkills);
        }

        [HttpGet("physical")]
        public ActionResult<IEnumerable<Skill>> GetPhysicalSkills()
        {
            var physicalAbilities = new[] { "STR", "DEX", "CON" };
            var skills = _skills.Where(s => physicalAbilities.Contains(s.Ability_Score.Name));
            return Ok(skills);
        }

        [HttpGet("mental")]
        public ActionResult<IEnumerable<Skill>> GetMentalSkills()
        {
            var mentalAbilities = new[] { "INT", "WIS" };
            var skills = _skills.Where(s => mentalAbilities.Contains(s.Ability_Score.Name));
            return Ok(skills);
        }

        [HttpGet("social")]
        public ActionResult<IEnumerable<Skill>> GetSocialSkills()
        {
            var skills = _skills.Where(s => s.Ability_Score.Name == "CHA");
            return Ok(skills);
        }

        [HttpGet("{index}/examples")]
        public ActionResult<List<string>> GetSkillExamples(string index)
        {
            var skill = _skills.FirstOrDefault(s => s.Index.Equals(index, StringComparison.OrdinalIgnoreCase));
            if (skill == null) return NotFound($"Skill '{index}' not found.");
            return Ok(SkillUtils.ExtractExamples(skill));
        }

        [HttpGet("{index}/full-description")]
        public ActionResult<string> GetSkillFullDescription(string index)
        {
            var skill = _skills.FirstOrDefault(s => s.Index.Equals(index, StringComparison.OrdinalIgnoreCase));
            if (skill == null) return NotFound($"Skill '{index}' not found.");
            return Ok(SkillUtils.GetFullDescription(skill));
        }

        [HttpGet("count")]
        public ActionResult<object> GetSkillsCount()
        {
            var countByAbility = SkillUtils.CountSkillsByAbility(_skills);
            return Ok(new { Total = _skills.Count, ByAbility = countByAbility });
        }

        [HttpGet("summary")]
        public ActionResult<object> GetSkillsSummary()
        {
            var summary = new
            {
                TotalSkills = _skills.Count,
                AbilityBreakdown = _skills.GroupBy(s => s.Ability_Score.Name)
                                          .Select(g => new
                                          {
                                              Ability = g.Key,
                                              Count = g.Count(),
                                              Skills = g.Select(s => s.Name).OrderBy(n => n)
                                          }),
                PhysicalSkills = _skills.Count(s => new[] { "STR", "DEX", "CON" }.Contains(s.Ability_Score.Name)),
                MentalSkills = _skills.Count(s => new[] { "INT", "WIS" }.Contains(s.Ability_Score.Name)),
                SocialSkills = _skills.Count(s => s.Ability_Score.Name == "CHA")
            };
            return Ok(summary);
        }
    }
}
