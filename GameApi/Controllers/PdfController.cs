using Microsoft.AspNetCore.Mvc;
using GameApi.Data;      // <- kell, hogy a PdfDbContext-et lássa
using GameApi.Models;    // <- kell, hogy a PdfFile típust lássa
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly PdfDbContext _context;

    public PdfController(IWebHostEnvironment env, PdfDbContext context)
    {
        _env = env;
        _context = context;
    }

    /// <summary>
    /// PDF fájl feltöltése
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPdf(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Nincs fájl!");

        var uploads = Path.Combine(_env.ContentRootPath, "UploadedPdfs");
        if (!Directory.Exists(uploads))
            Directory.CreateDirectory(uploads);

        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploads, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var pdfRecord = new PdfFile
        {
            FileName = file.FileName,
            FilePath = filePath,
            UploadedAt = DateTime.Now
        };

        _context.PdfFiles.Add(pdfRecord);
        await _context.SaveChangesAsync();

        return Ok(new { id = pdfRecord.Id });
    }

    /// <summary>
    /// PDF letöltése ID alapján
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetPdf(int id)
    {
        var pdf = _context.PdfFiles.Find(id);
        if (pdf == null) return NotFound();

        var fileBytes = System.IO.File.ReadAllBytes(pdf.FilePath);
        return File(fileBytes, "application/pdf", pdf.FileName);
    }
}
