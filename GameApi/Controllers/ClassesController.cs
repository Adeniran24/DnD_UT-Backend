using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClassesController : ControllerBase
    {
        private readonly string _jsonPath;

        public ClassesController()
        {
            _jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Database/2014/5e-SRD-Classes.json");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!System.IO.File.Exists(_jsonPath))
                return NotFound("Classes JSON file not found.");

            var jsonString = await System.IO.File.ReadAllTextAsync(_jsonPath);
            return Content(jsonString, "application/json");
        }

        [HttpGet("{index}")]
        public async Task<IActionResult> GetByIndex(string index)
        {
            if (!System.IO.File.Exists(_jsonPath))
                return NotFound("Classes JSON file not found.");

            var jsonString = await System.IO.File.ReadAllTextAsync(_jsonPath);
            var jsonArray = JsonNode.Parse(jsonString)?.AsArray();

            if (jsonArray == null) return NotFound();

            // LINQ to find the matching class
            var classNode = jsonArray.FirstOrDefault(c => c?["index"]?.ToString() == index);

            if (classNode == null) return NotFound();

            return Content(classNode.ToJsonString(), "application/json");
        }
    }
}
