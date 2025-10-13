/*using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using GameApi.Models.DND2014;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/2014/[controller]")]
    public class FeaturesController : ControllerBase
    {
        private static readonly List<Feature> _features;
        private static readonly string _filePath;

        static FeaturesController()
        {
            try
            {
                // Path relative to project root (like other controllers)
                _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "2014", "5e-SRD-Features.json");

                if (File.Exists(_filePath))
                {
                    var jsonString = File.ReadAllText(_filePath);
                    _features = JsonSerializer.Deserialize<List<Feature>>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Feature>();
                }
                else
                {
                    Console.WriteLine($"Features file not found at {_filePath}");
                    _features = new List<Feature>();
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error loading features: {ex.Message}");
                _features = new List<Feature>();
            }
        }

        // GET: api/2014/features
        [HttpGet]
        public IActionResult GetAll(
            [FromQuery] string? className = null,
            [FromQuery] string? subclassName = null,
            [FromQuery] int? level = null,
            [FromQuery] string? search = null)
        {
            if (!_features.Any())
                return NotFound("No features available.");

            var query = _features.AsQueryable();

            if (!string.IsNullOrEmpty(className))
                query = query.Where(f => f.Class.Name.Contains(className, System.StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(subclassName) && subclassName != "null")
                query = query.Where(f => f.Subclass != null &&
                                         f.Subclass.Name.Contains(subclassName, System.StringComparison.OrdinalIgnoreCase));

            if (level.HasValue)
                query = query.Where(f => f.Level == level.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(f => f.Name.Contains(search, System.StringComparison.OrdinalIgnoreCase) ||
                                         f.Desc.Any(d => d.Contains(search, System.StringComparison.OrdinalIgnoreCase)));

            return Ok(query.ToList());
        }

        // GET: api/2014/features/{index}
        [HttpGet("{index}")]
        public IActionResult GetByIndex(string index)
        {
            if (!_features.Any())
                return NotFound("No features available.");

            var feature = _features.FirstOrDefault(f =>
                f.Index.Equals(index, System.StringComparison.OrdinalIgnoreCase));

            if (feature == null)
                return NotFound($"Feature with index '{index}' not found.");

            return Ok(feature);
        }

        // GET: api/2014/features/all
        [HttpGet("all")]
        public IActionResult GetAllFeatures()
        {
            if (!_features.Any())
                return NotFound("No features available.");

            return Ok(_features);
        }

        // GET: api/2014/features/download
        [HttpGet("download")]
        public IActionResult DownloadAllFeatures()
        {
            if (!_features.Any())
                return NotFound("Features file not found or empty.");

            if (!File.Exists(_filePath))
                return NotFound("Features file not found.");

            byte[] fileBytes = System.IO.File.ReadAllBytes(_filePath);

            // Correct overload: File(byte[] fileContents, string contentType, string fileDownloadName)
            return File(fileBytes, "application/json", "5e-SRD-Features.json");
        }
    }
}
*/