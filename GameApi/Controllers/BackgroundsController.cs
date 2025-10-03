using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using GameApi.DTOs.DND2014;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/2014/[controller]")]
    public class BackgroundsController : ControllerBase
    {
        private static readonly List<CharacterBackground> _backgrounds;

        static BackgroundsController()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "2014", "5e-SRD-Backgrounds.json");

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    _backgrounds = JsonSerializer.Deserialize<List<CharacterBackground>>(jsonString) ?? new List<CharacterBackground>();
                }
                catch (Exception ex)
                {
                    // Log the exception here if you have logging configured
                    Console.WriteLine($"Error loading backgrounds: {ex.Message}");
                    _backgrounds = new List<CharacterBackground>();
                }
            }
            else
            {
                _backgrounds = new List<CharacterBackground>();
            }
        }

        // GET: api/2014/backgrounds
        [HttpGet]
        public IActionResult GetAll()
        {
            if (!_backgrounds.Any())
                return NotFound("Background data not found.");

            return Ok(_backgrounds);
        }

        // GET: api/2014/backgrounds/{index}
        [HttpGet("{index}")]
        public IActionResult GetByIndex(string index)
        {
            var background = _backgrounds.FirstOrDefault(b =>
                b.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (background == null)
                return NotFound($"Background with index '{index}' not found.");

            return Ok(background);
        }

        // GET: api/2014/backgrounds/{index}/feature
        [HttpGet("{index}/feature")]
        public IActionResult GetFeature(string index)
        {
            var background = _backgrounds.FirstOrDefault(b =>
                b.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (background == null)
                return NotFound($"Background with index '{index}' not found.");

            if (background.Feature == null)
                return NotFound($"Feature not found for background '{index}'.");

            return Ok(background.Feature);
        }

        // GET: api/2014/backgrounds/{index}/starting-equipment
        [HttpGet("{index}/starting-equipment")]
        public IActionResult GetStartingEquipment(string index)
        {
            var background = _backgrounds.FirstOrDefault(b =>
                b.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (background == null)
                return NotFound($"Background with index '{index}' not found.");

            return Ok(new
            {
                Equipment = background.StartingEquipment,
                EquipmentOptions = background.StartingEquipmentOptions
            });
        }

        // GET: api/2014/backgrounds/{index}/proficiencies
        [HttpGet("{index}/proficiencies")]
        public IActionResult GetProficiencies(string index)
        {
            var background = _backgrounds.FirstOrDefault(b =>
                b.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (background == null)
                return NotFound($"Background with index '{index}' not found.");

            return Ok(background.StartingProficiencies);
        }
    }
}