// Controllers/FeaturesController.cs
using Microsoft.AspNetCore.Mvc;
using DndFeaturesApp.Models;
using System.Text.Json;

namespace DndFeaturesApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeaturesController : ControllerBase
    {
        private readonly List<Feature> _features;
        private readonly ILogger<FeaturesController> _logger;
        private readonly string _jsonFilePath;

        public FeaturesController(ILogger<FeaturesController> logger, IWebHostEnvironment env)
        {
            _logger = logger;

            _jsonFilePath = Path.Combine(env.ContentRootPath, "Database", "2014", "5e-SRD-Features.json");

            _features = LoadFeaturesFromJsonFile();
        }

        [HttpGet]
        public ActionResult<IEnumerable<Feature>> GetFeatures() =>
            Ok(_features);

        [HttpGet("{index}")]
        public ActionResult<Feature> GetFeature(string index)
        {
            var feature = _features.FirstOrDefault(f =>
                f.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (feature == null) return NotFound($"Feature with index '{index}' not found");

            return Ok(feature);
        }

        [HttpGet("class/{className}")]
        public ActionResult<IEnumerable<Feature>> GetFeaturesByClass(string className)
        {
            var features = _features.Where(f =>
                f.Class.Index.Equals(className, StringComparison.OrdinalIgnoreCase) ||
                f.Class.Name.Equals(className, StringComparison.OrdinalIgnoreCase));

            return Ok(features);
        }

        [HttpGet("class/{className}/level/{level}")]
        public ActionResult<IEnumerable<Feature>> GetFeaturesByClassAndLevel(string className, int level)
        {
            var features = _features.Where(f =>
                (f.Class.Index.Equals(className, StringComparison.OrdinalIgnoreCase) ||
                 f.Class.Name.Equals(className, StringComparison.OrdinalIgnoreCase)) &&
                f.Level == level);

            return Ok(features);
        }

        [HttpGet("subclass/{subclassName}")]
        public ActionResult<IEnumerable<Feature>> GetFeaturesBySubclass(string subclassName)
        {
            var features = _features.Where(f =>
                f.Subclass != null &&
                (f.Subclass.Index.Equals(subclassName, StringComparison.OrdinalIgnoreCase) ||
                 f.Subclass.Name.Equals(subclassName, StringComparison.OrdinalIgnoreCase)));

            return Ok(features);
        }

        [HttpGet("search")]
        public ActionResult<IEnumerable<Feature>> SearchFeatures([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Search term cannot be empty");

            var features = _features.Where(f =>
                f.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                f.Index.Contains(name, StringComparison.OrdinalIgnoreCase));

            return Ok(features);
        }

        [HttpGet("levels")]
        public ActionResult<IEnumerable<int>> GetAvailableLevels() =>
            Ok(_features.Select(f => f.Level).Distinct().OrderBy(l => l));

        [HttpGet("classes")]
        public ActionResult<IEnumerable<string>> GetAvailableClasses() =>
            Ok(_features.Select(f => f.Class.Name).Distinct().OrderBy(c => c));

        [HttpGet("subclasses")]
        public ActionResult<IEnumerable<string>> GetAvailableSubclasses() =>
            Ok(_features.Where(f => f.Subclass != null)
                        .Select(f => f.Subclass!.Name)
                        .Distinct()
                        .OrderBy(c => c));

        [HttpGet("class/{className}/subclasses")]
        public ActionResult<IEnumerable<string>> GetSubclassesByClass(string className) =>
            Ok(_features.Where(f =>
                    (f.Class.Index.Equals(className, StringComparison.OrdinalIgnoreCase) ||
                     f.Class.Name.Equals(className, StringComparison.OrdinalIgnoreCase)) &&
                    f.Subclass != null)
                .Select(f => f.Subclass!.Name)
                .Distinct()
                .OrderBy(c => c));

        [HttpGet("parent/{parentIndex}")]
        public ActionResult<IEnumerable<Feature>> GetFeaturesByParent(string parentIndex) =>
            Ok(_features.Where(f => f.Parent != null &&
                                     f.Parent.Index.Equals(parentIndex, StringComparison.OrdinalIgnoreCase)));

        [HttpGet("count")]
        public ActionResult<FeatureCount> GetFeatureCount()
        {
            var count = new FeatureCount
            {
                TotalFeatures = _features.Count,
                FeaturesByClass = _features.GroupBy(f => f.Class.Name)
                                           .ToDictionary(g => g.Key, g => g.Count()),
                FeaturesByLevel = _features.GroupBy(f => f.Level)
                                           .ToDictionary(g => g.Key, g => g.Count())
            };

            return Ok(count);
        }

        private List<Feature> LoadFeaturesFromJsonFile()
        {
            try
            {
                if (!System.IO.File.Exists(_jsonFilePath))
                    throw new FileNotFoundException($"Features JSON file not found at: {_jsonFilePath}");

                var jsonData = System.IO.File.ReadAllText(_jsonFilePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<Feature>>(jsonData, options) ?? new List<Feature>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading features from JSON file");
                throw new InvalidOperationException($"Error loading features data: {ex.Message}", ex);
            }
        }
    }

    public class FeatureCount
    {
        public int TotalFeatures { get; set; }
        public Dictionary<string, int> FeaturesByClass { get; set; } = new();
        public Dictionary<int, int> FeaturesByLevel { get; set; } = new();
    }
}
