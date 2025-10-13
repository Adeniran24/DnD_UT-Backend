// Controllers/RulesController.cs
using Microsoft.AspNetCore.Mvc;
using DnDAPI.Models;
using System.Text.Json;

namespace DnDAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RulesController : ControllerBase
    {
        private readonly List<Rule> _rules;
        private readonly ILogger<RulesController> _logger;

        public RulesController(ILogger<RulesController> logger)
        {
            _logger = logger;
            _rules = LoadRulesFromFile();
        }

        private List<Rule> LoadRulesFromFile()
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "2014", "5e-SRD-Rules.json");
                
                _logger.LogInformation("Looking for rules file at: {FilePath}", filePath);
                
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("Rules file not found at: {FilePath}", filePath);
                    return new List<Rule>();
                }

                var jsonData = System.IO.File.ReadAllText(filePath);
                _logger.LogInformation("Successfully loaded rules JSON data, length: {Length}", jsonData.Length);

                var rules = JsonSerializer.Deserialize<List<Rule>>(jsonData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Deserialized {Count} rules", rules?.Count ?? 0);
                return rules ?? new List<Rule>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading rules from file");
                return new List<Rule>();
            }
        }

        // GET: api/rules
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Rule>> GetAllRules()
        {
            _logger.LogInformation("Returning {Count} rules", _rules.Count);
            return Ok(_rules);
        }

        // GET: api/rules/{index}
        [HttpGet("{index}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Rule> GetRule(string index)
        {
            var rule = _rules.FirstOrDefault(r => 
                r.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (rule == null)
            {
                return NotFound($"Rule with index '{index}' not found.");
            }

            return Ok(rule);
        }

        // GET: api/rules/search?name={name}
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<IEnumerable<Rule>> SearchRules([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name parameter is required.");
            }

            var rules = _rules.Where(r => 
                r.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(rules);
        }

        // GET: api/rules/search/description?keyword={keyword}
        [HttpGet("search/description")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<IEnumerable<Rule>> SearchRulesByDescription([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest("Keyword parameter is required.");
            }

            var rules = _rules.Where(r => 
                r.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(rules);
        }

        // GET: api/rules/categories
        [HttpGet("categories")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<string>> GetRuleCategories()
        {
            // Extract potential categories from rule names (first word or common patterns)
            var categories = _rules
                .Select(r => r.Name.Split(' ').FirstOrDefault() ?? "")
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return Ok(categories);
        }

        // GET: api/rules/{index}/html
        [HttpGet("{index}/html")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<string> GetRuleAsHtml(string index)
        {
            var rule = _rules.FirstOrDefault(r => 
                r.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (rule == null)
            {
                return NotFound($"Rule with index '{index}' not found.");
            }

            // The description already contains markdown formatting that can be rendered as HTML
            // In a real application, you might want to convert markdown to HTML here
            return Ok(rule.Description);
        }

        // GET: api/rules/combat
        [HttpGet("combat")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Rule>> GetCombatRules()
        {
            var combatRules = _rules.Where(r => 
                r.Name.Contains("Combat", StringComparison.OrdinalIgnoreCase) ||
                r.Index.Contains("combat", StringComparison.OrdinalIgnoreCase) ||
                r.Description.Contains("combat", StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(combatRules);
        }

        // GET: api/rules/magic
        [HttpGet("magic")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Rule>> GetMagicRules()
        {
            var magicRules = _rules.Where(r => 
                r.Name.Contains("Spell", StringComparison.OrdinalIgnoreCase) ||
                r.Name.Contains("Magic", StringComparison.OrdinalIgnoreCase) ||
                r.Index.Contains("spell", StringComparison.OrdinalIgnoreCase) ||
                r.Description.Contains("spell", StringComparison.OrdinalIgnoreCase) ||
                r.Description.Contains("magic", StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(magicRules);
        }

        // GET: api/rules/ability
        [HttpGet("ability")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Rule>> GetAbilityRules()
        {
            var abilityRules = _rules.Where(r => 
                r.Name.Contains("Ability", StringComparison.OrdinalIgnoreCase) ||
                r.Index.Contains("ability", StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(abilityRules);
        }
    }
}