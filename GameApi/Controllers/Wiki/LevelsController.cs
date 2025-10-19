using Microsoft.AspNetCore.Mvc;
using DndFeaturesApp.Models;
using System.Text.Json;

namespace DndFeaturesApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LevelsController : ControllerBase
    {
        private readonly List<Level> _levels;
        private readonly ILogger<LevelsController> _logger;
        private readonly string _jsonFilePath;

        public LevelsController(ILogger<LevelsController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _jsonFilePath = Path.Combine(env.ContentRootPath, "Database", "2014", "5e-SRD-Levels.json");
            _levels = LoadLevelsFromJsonFile();
        }

        [HttpGet]
        public ActionResult<IEnumerable<Level>> GetLevels() => Ok(_levels);

        [HttpGet("{index}")]
        public ActionResult<Level> GetLevel(string index)
        {
            var level = _levels.FirstOrDefault(l => l.Index.Equals(index, StringComparison.OrdinalIgnoreCase));
            if (level == null) return NotFound($"Level with index '{index}' not found");
            return Ok(level);
        }

        [HttpGet("class/{className}")]
        public ActionResult<IEnumerable<Level>> GetLevelsByClass(string className)
        {
            var levels = _levels.Where(l => l.Class.Index.Equals(className, StringComparison.OrdinalIgnoreCase) ||
                                            l.Class.Name.Equals(className, StringComparison.OrdinalIgnoreCase));
            return Ok(levels);
        }

        [HttpGet("class/{className}/level/{levelNumber}")]
        public ActionResult<Level> GetLevelByClassAndNumber(string className, int levelNumber)
        {
            var level = _levels.FirstOrDefault(l => 
                (l.Class.Index.Equals(className, StringComparison.OrdinalIgnoreCase) ||
                 l.Class.Name.Equals(className, StringComparison.OrdinalIgnoreCase)) &&
                 l.LevelNumber == levelNumber);
            
            if (level == null) return NotFound($"Level {levelNumber} for class '{className}' not found");
            return Ok(level);
        }

        private List<Level> LoadLevelsFromJsonFile()
        {
            try
            {
                if (!System.IO.File.Exists(_jsonFilePath))
                    throw new FileNotFoundException($"Levels JSON file not found at: {_jsonFilePath}");

                var jsonData = System.IO.File.ReadAllText(_jsonFilePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<Level>>(jsonData, options) ?? new List<Level>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading levels from JSON file");
                throw new InvalidOperationException($"Error loading levels data: {ex.Message}", ex);
            }
        }
    }
}
