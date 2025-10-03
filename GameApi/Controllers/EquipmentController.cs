using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using GameApi.Models.DND2014;
using GameApi.DTOs.DND2014;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/2014/[controller]")]
    public class EquipmentController : ControllerBase
    {
        private static readonly List<EquipmentItem> _equipmentItems;

        static EquipmentController()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "2014", "5e-SRD-Equipment.json");

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    _equipmentItems = JsonSerializer.Deserialize<List<EquipmentItem>>(jsonString) ?? new List<EquipmentItem>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading equipment: {ex.Message}");
                    _equipmentItems = new List<EquipmentItem>();
                }
            }
            else
            {
                _equipmentItems = new List<EquipmentItem>();
            }
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            if (!_equipmentItems.Any())
                return NotFound("Equipment data not found.");

            return Ok(_equipmentItems);
        }

        [HttpGet("{index}")]
        public IActionResult GetByIndex(string index)
        {
            var item = _equipmentItems.FirstOrDefault(e =>
                e.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (item == null)
                return NotFound($"Equipment with index '{index}' not found.");

            return Ok(item);
        }

        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            var categories = _equipmentItems
                .Select(e => e.EquipmentCategory?.Index)
                .Where(c => c != null)
                .Distinct()
                .ToList();

            return Ok(categories);
        }

        [HttpGet("category/{category}")]
        public IActionResult GetByCategory(string category)
        {
            var items = _equipmentItems
                .Where(e => e.EquipmentCategory?.Index?.Equals(category, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            if (!items.Any())
                return NotFound($"No equipment found in category '{category}'.");

            return Ok(items);
        }

        [HttpGet("weapons")]
        public IActionResult GetWeapons([FromQuery] string? weaponCategory = null)
        {
            var weapons = _equipmentItems
                .Where(e => e.EquipmentCategory?.Index == "weapon");

            if (!string.IsNullOrEmpty(weaponCategory))
            {
                weapons = weapons.Where(e => e.WeaponCategory?.Equals(weaponCategory, StringComparison.OrdinalIgnoreCase) == true);
            }

            var result = weapons.ToList();
            if (!result.Any())
                return NotFound("No weapons found.");

            return Ok(result);
        }

        [HttpGet("armor")]
        public IActionResult GetArmor([FromQuery] string? armorCategory = null)
        {
            var armor = _equipmentItems
                .Where(e => e.EquipmentCategory?.Index == "armor");

            if (!string.IsNullOrEmpty(armorCategory))
            {
                armor = armor.Where(e => e.ArmorCategory?.Equals(armorCategory, StringComparison.OrdinalIgnoreCase) == true);
            }

            var result = armor.ToList();
            if (!result.Any())
                return NotFound("No armor found.");

            return Ok(result);
        }

        [HttpGet("gear")]
        public IActionResult GetGear([FromQuery] string? gearCategory = null)
        {
            var gear = _equipmentItems
                .Where(e => e.EquipmentCategory?.Index == "adventuring-gear");

            if (!string.IsNullOrEmpty(gearCategory))
            {
                gear = gear.Where(e => e.GearCategory?.Index == gearCategory);
            }

            var result = gear.ToList();
            if (!result.Any())
                return NotFound("No gear found.");

            return Ok(result);
        }

        [HttpGet("tools")]
        public IActionResult GetTools([FromQuery] string? toolCategory = null)
        {
            var tools = _equipmentItems
                .Where(e => e.EquipmentCategory?.Index == "tools");

            if (!string.IsNullOrEmpty(toolCategory))
            {
                tools = tools.Where(e => e.ToolCategory?.Equals(toolCategory, StringComparison.OrdinalIgnoreCase) == true);
            }

            var result = tools.ToList();
            if (!result.Any())
                return NotFound("No tools found.");

            return Ok(result);
        }

        [HttpGet("mounts-vehicles")]
        public IActionResult GetMountsAndVehicles([FromQuery] string? vehicleCategory = null)
        {
            var mountsVehicles = _equipmentItems
                .Where(e => e.EquipmentCategory?.Index == "mounts-and-vehicles");

            if (!string.IsNullOrEmpty(vehicleCategory))
            {
                mountsVehicles = mountsVehicles.Where(e => e.VehicleCategory?.Equals(vehicleCategory, StringComparison.OrdinalIgnoreCase) == true);
            }

            var result = mountsVehicles.ToList();
            if (!result.Any())
                return NotFound("No mounts or vehicles found.");

            return Ok(result);
        }

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Search query is required.");

            var results = _equipmentItems
                .Where(e => e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                           (e.Description != null && e.Description.Any(d => d.Contains(q, StringComparison.OrdinalIgnoreCase))))
                .ToList();

            if (!results.Any())
                return NotFound($"No equipment found matching '{q}'.");

            return Ok(results);
        }
    }
}