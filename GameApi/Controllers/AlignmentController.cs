using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using GameApi.DTOs.DND2014;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/2014/[controller]")]
    public class AlignmentsController : ControllerBase
    {
        private static readonly List<Alignment> _alignments;

        static AlignmentsController()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "2014", "5e-SRD-Alignments.json");

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    _alignments = JsonSerializer.Deserialize<List<Alignment>>(jsonString) ?? new List<Alignment>();
                }
                catch
                {
                    _alignments = new List<Alignment>();
                }
            }
            else
            {
                _alignments = new List<Alignment>();
            }
        }

        // GET: api/2014/alignments
        [HttpGet]
        public IActionResult GetAll()
        {
            if (!_alignments.Any())
                return NotFound("Alignment data not found.");

            return Ok(_alignments);
        }

        // GET: api/2014/alignments/{index}
        [HttpGet("{index}")]
        public IActionResult GetByIndex(string index)
        {
            var alignment = _alignments.FirstOrDefault(a =>
                a.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (alignment == null)
                return NotFound($"Alignment with index '{index}' not found.");

            return Ok(alignment);
        }
    }
}
