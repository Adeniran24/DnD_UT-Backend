using Microsoft.AspNetCore.Mvc;
using GameApi.Data;
using GameApi.Models;
using Microsoft.EntityFrameworkCore;
using iText.Kernel.Pdf;
using iText.Forms;
using iText.Forms.Fields;
using Newtonsoft.Json;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly AppDbContext _context;

    public PdfController(IWebHostEnvironment env, AppDbContext context)
    {
        _env = env;
        _context = context;
    }

    // -------------------------------
    // 1️⃣ Upload a new PDF (optional)
    // -------------------------------
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPdf(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded!");

        var uploads = Path.Combine(_env.ContentRootPath, "UploadedPdfs");
        if (!Directory.Exists(uploads))
            Directory.CreateDirectory(uploads);

        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploads, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

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

    // -------------------------------
    // 2️⃣ Get the template PDF
    // -------------------------------
    [HttpGet("template")]
    public IActionResult GetTemplatePdf()
    {
        var templatePath = Path.Combine(_env.ContentRootPath, "PdfTemplates", "CharacterSheetTemplate.pdf");
        if (!System.IO.File.Exists(templatePath))
            return NotFound("Template PDF not found.");

        var fileBytes = System.IO.File.ReadAllBytes(templatePath);
        return File(fileBytes, "application/pdf", "CharacterSheetTemplate.pdf");
    }

    // -------------------------------
    // 3️⃣ Save user form data as JSON
    // -------------------------------
    [HttpPost("save/{id}")]
    public async Task<IActionResult> SaveFormData(int id, [FromBody] Dictionary<string, string> fields)
    {
        var pdfRecord = await _context.PdfFiles.FirstOrDefaultAsync(x => x.Id == id);
        if (pdfRecord == null) return NotFound();

        var record = await _context.PdfFormDatas.FirstOrDefaultAsync(x => x.PdfFileId == id);
        if (record == null)
        {
            record = new PdfFormData
            {
                PdfFileId = id,
                FieldValuesJson = JsonConvert.SerializeObject(fields),
                SavedAt = DateTime.Now
            };
            _context.PdfFormDatas.Add(record);
        }
        else
        {
            record.FieldValuesJson = JsonConvert.SerializeObject(fields);
            record.SavedAt = DateTime.Now;
            _context.PdfFormDatas.Update(record);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Form data saved successfully." });
    }

    // -------------------------------
    // 4️⃣ Get filled PDF for editing
    // -------------------------------
    [HttpGet("final/{id}")]
    public IActionResult GetFilledPdf(int id)
    {
        var pdfRecord = _context.PdfFiles.FirstOrDefault(x => x.Id == id);
        if (pdfRecord == null) return NotFound();

        var dataRecord = _context.PdfFormDatas.FirstOrDefault(x => x.PdfFileId == id);
        Dictionary<string, string> fieldData = new Dictionary<string, string>();
        if (dataRecord != null)
            fieldData = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataRecord.FieldValuesJson);

        var templatePath = Path.Combine(_env.ContentRootPath, "PdfTemplates", "CharacterSheetTemplate.pdf");
        if (!System.IO.File.Exists(templatePath))
            return NotFound("Template PDF not found.");

        using (var reader = new PdfReader(templatePath))
        using (var memoryStream = new MemoryStream())
        using (var writer = new PdfWriter(memoryStream))
        using (var pdfDoc = new PdfDocument(reader, writer))
        {
            var form = PdfAcroForm.GetAcroForm(pdfDoc, true);

            // Fill fields
            foreach (var entry in fieldData)
            {
                var field = form.GetField(entry.Key);
                if (field != null)
                    field.SetValue(entry.Value);
            }

            // Do NOT flatten fields so they remain editable in WebViewer
            pdfDoc.Close();

            var bytes = memoryStream.ToArray();
            return File(bytes, "application/pdf", $"Player_{id}.pdf");
        }
    }
}
