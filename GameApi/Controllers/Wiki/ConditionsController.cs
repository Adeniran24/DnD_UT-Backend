using GameApi.Models.DND2014;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConditionsController : ControllerBase
    {
        private readonly string _jsonPath;

        public ConditionsController()
        {
            // Use AppContext.BaseDirectory to get the correct running directory
            _jsonPath = Path.Combine(AppContext.BaseDirectory, "Database/2014/5e-SRD-Conditions.json");
        }

        // GET: api/conditions
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!System.IO.File.Exists(_jsonPath))
                return NotFound($"Conditions JSON file not found at path: {_jsonPath}");

            var jsonString = await System.IO.File.ReadAllTextAsync(_jsonPath);

            // Debugging: log length of JSON content
            if (string.IsNullOrWhiteSpace(jsonString))
                return NotFound("Conditions JSON file is empty.");

            List<Condition>? conditions;
            try
            {
                conditions = JsonSerializer.Deserialize<List<Condition>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                return BadRequest($"Failed to parse JSON: {ex.Message}");
            }

            if (conditions == null || !conditions.Any())
                return NotFound("No conditions found in the JSON file.");

            return Ok(conditions);
        }

        // GET: api/conditions/{index}
        [HttpGet("{index}")]
        public async Task<IActionResult> GetByIndex(string index)
        {
            if (!System.IO.File.Exists(_jsonPath))
                return NotFound($"Conditions JSON file not found at path: {_jsonPath}");

            var jsonString = await System.IO.File.ReadAllTextAsync(_jsonPath);

            if (string.IsNullOrWhiteSpace(jsonString))
                return NotFound("Conditions JSON file is empty.");

            List<Condition>? conditions;
            try
            {
                conditions = JsonSerializer.Deserialize<List<Condition>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                return BadRequest($"Failed to parse JSON: {ex.Message}");
            }

            var condition = conditions?.FirstOrDefault(c => c.Index == index);

            if (condition == null)
                return NotFound($"Condition with index '{index}' not found.");

            return Ok(condition);
        }
    }
}
