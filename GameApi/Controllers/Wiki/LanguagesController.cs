using Microsoft.AspNetCore.Mvc;
using DndFeaturesApp.Models;
using System.Text.Json;

namespace DndFeaturesApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LanguagesController : ControllerBase
    {
        private readonly List<Language> _languages;
        private readonly ILogger<LanguagesController> _logger;
        private readonly string _jsonFilePath;

        public LanguagesController(ILogger<LanguagesController> logger, IWebHostEnvironment env)
        {
            _logger = logger;

            _jsonFilePath = Path.Combine(env.ContentRootPath, "Database", "2014", "5e-SRD-Languages.json");

            _languages = LoadLanguagesFromJsonFile();
        }

        [HttpGet]
        public ActionResult<IEnumerable<Language>> GetLanguages() => Ok(_languages);

        [HttpGet("{index}")]
        public ActionResult<Language> GetLanguage(string index)
        {
            var language = _languages.FirstOrDefault(l => l.Index.Equals(index, StringComparison.OrdinalIgnoreCase));
            if (language == null) return NotFound($"Language with index '{index}' not found");
            return Ok(language);
        }

        [HttpGet("type/{type}")]
        public ActionResult<IEnumerable<Language>> GetLanguagesByType(string type)
        {
            var langs = _languages.Where(l => l.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
            return Ok(langs);
        }

        [HttpGet("script/{script}")]
        public ActionResult<IEnumerable<Language>> GetLanguagesByScript(string script)
        {
            var langs = _languages.Where(l => l.Script != null && l.Script.Equals(script, StringComparison.OrdinalIgnoreCase));
            return Ok(langs);
        }

        private List<Language> LoadLanguagesFromJsonFile()
        {
            try
            {
                if (!System.IO.File.Exists(_jsonFilePath))
                    throw new FileNotFoundException($"Languages JSON file not found at: {_jsonFilePath}");

                var jsonData = System.IO.File.ReadAllText(_jsonFilePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<Language>>(jsonData, options) ?? new List<Language>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading languages from JSON file");
                throw new InvalidOperationException($"Error loading languages data: {ex.Message}", ex);
            }
        }
    }
}
