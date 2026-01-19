using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GameApi.Controllers
{
    // Model representing a weapon property
    public class WeaponProperty
    {
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("desc")] // Map JSON "desc" to Description
        public List<string> Description { get; set; } = new();

        public string Url { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/2014/[controller]")]
    public class WeaponPropertiesController : ControllerBase
    {
        private readonly string _jsonFilePath;
        private List<WeaponProperty>? _cachedWeaponProperties;

        public WeaponPropertiesController(IWebHostEnvironment env)
        {
            _jsonFilePath = Path.Combine(env.ContentRootPath, "Database", "2014", "5e-SRD-Weapon-Properties.json");
        }

        private async Task<List<WeaponProperty>?> LoadWeaponPropertiesAsync()
        {
            if (_cachedWeaponProperties != null)
                return _cachedWeaponProperties;

            if (!System.IO.File.Exists(_jsonFilePath))
                return null;

            var json = await System.IO.File.ReadAllTextAsync(_jsonFilePath);
            _cachedWeaponProperties = JsonSerializer.Deserialize<List<WeaponProperty>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<WeaponProperty>();

            foreach (var wp in _cachedWeaponProperties)
            {
                wp.Description ??= new List<string>();
            }

            return _cachedWeaponProperties;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WeaponProperty>>> Get()
        {
            var weaponProperties = await LoadWeaponPropertiesAsync();
            if (weaponProperties == null)
                return NotFound("Weapon properties JSON file not found.");

            return Ok(weaponProperties);
        }

        [HttpGet("{index}")]
        public async Task<ActionResult<WeaponProperty>> GetByIndex(string index)
        {
            var weaponProperties = await LoadWeaponPropertiesAsync();
            if (weaponProperties == null)
                return NotFound("Weapon properties JSON file not found.");

            var property = weaponProperties.Find(p => p.Index == index);

            if (property == null)
                return NotFound($"Weapon property '{index}' not found.");

            return Ok(property);
        }
    }
}
