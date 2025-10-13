using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[Route("api/[controller]")]
[ApiController]
public class MagicSchoolsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MagicSchoolsController> _logger;
    private readonly string _jsonPath;

    public MagicSchoolsController(ILogger<MagicSchoolsController> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
        _jsonPath = Path.Combine(_env.ContentRootPath, "Database", "2014", "magic-schools.json");
    }

    private List<MagicSchool> LoadMagicSchoolsFromJsonFile()
    {
        if (!System.IO.File.Exists(_jsonPath))
            throw new InvalidOperationException($"Magic schools JSON file not found at: {_jsonPath}");

        var jsonString = System.IO.File.ReadAllText(_jsonPath);
        return JsonSerializer.Deserialize<List<MagicSchool>>(jsonString) 
               ?? new List<MagicSchool>();
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        try
        {
            var schools = LoadMagicSchoolsFromJsonFile();
            return Ok(schools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading magic schools data");
            return StatusCode(500, $"Error loading magic schools data: {ex.Message}");
        }
    }

    [HttpGet("{index}")]
    public IActionResult GetByIndex(string index)
    {
        try
        {
            var schools = LoadMagicSchoolsFromJsonFile();
            var school = schools.FirstOrDefault(s => s.Index.Equals(index, StringComparison.OrdinalIgnoreCase));

            if (school == null)
                return NotFound($"Magic school with index '{index}' not found.");

            return Ok(school);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading magic schools data");
            return StatusCode(500, $"Error loading magic schools data: {ex.Message}");
        }
    }
}
