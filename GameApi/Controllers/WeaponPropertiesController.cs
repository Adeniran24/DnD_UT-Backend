using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameApi.Controllers
{
    // Model representing a weapon property
    public class WeaponProperty
    {
        public string Index { get; set; }
        public string Name { get; set; }
        public List<string> Description { get; set; }
        public string Url { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class WeaponPropertiesController : ControllerBase
    {
        private readonly string _jsonFilePath;

        public WeaponPropertiesController(IWebHostEnvironment env)
        {
            // Path to your JSON file relative to the project root
            _jsonFilePath = Path.Combine(env.ContentRootPath, "Database", "2014", "5e-SRD-Weapon-Properties.json");
        }

        // GET api/weaponproperties
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WeaponProperty>>> Get()
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return NotFound("Weapon properties JSON file not found.");
            }

            try
            {
                var json = await System.IO.File.ReadAllTextAsync(_jsonFilePath);
                var weaponProperties = JsonSerializer.Deserialize<List<WeaponProperty>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return Ok(weaponProperties);
            }
            catch (JsonException ex)
            {
                return BadRequest($"Error parsing JSON: {ex.Message}");
            }
        }

        // GET api/weaponproperties/{index}
        [HttpGet("{index}")]
        public async Task<ActionResult<WeaponProperty>> GetByIndex(string index)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return NotFound("Weapon properties JSON file not found.");
            }

            var json = await System.IO.File.ReadAllTextAsync(_jsonFilePath);
            var weaponProperties = JsonSerializer.Deserialize<List<WeaponProperty>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var property = weaponProperties?.Find(p => p.Index == index);

            if (property == null)
                return NotFound($"Weapon property '{index}' not found.");

            return Ok(property);
        }
    }
}
