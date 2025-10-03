using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using GameApi.DTOs.DND2014;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/2014/[controller]")]
    public class DamageTypesController : ControllerBase
    {
        private static readonly List<DamageType> _damageTypes;

        static DamageTypesController()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "2014", "5e-SRD-Damage-Types.json");

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    _damageTypes = JsonSerializer.Deserialize<List<DamageType>>(jsonString) ?? new List<DamageType>();
                }
                catch
                {
                    _damageTypes = new List<DamageType>();
                }
            }
            else
            {
                _damageTypes = new List<DamageType>();
            }
        }

        // GET: api/2014/damage-types
        [HttpGet]
        public IActionResult GetAll()
        {
            if (!_damageTypes.Any())
                return NotFound("Damage type data not found.");

            return Ok(_damageTypes);
        }

        // GET: api/2014/damage-types/{index}
        [HttpGet("{index}")]
        public IActionResult GetByIndex(string index)
        {
            var damageType = _damageTypes.FirstOrDefault(d =>
                d.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (damageType == null)
                return NotFound($"Damage type with index '{index}' not found.");

            return Ok(damageType);
        }
    }
}
