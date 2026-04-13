using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameApi.Data;
using GameApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

    private static readonly Regex CoverImageRegex =
        new(@"!\[[^\]]*cover[^\]]*\]\(([^)]+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public BooksController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [NonAction]
        public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
        {
            return await _context.Books.ToListAsync();
        }

        [NonAction]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            return book;
        }

        [HttpGet("markdown")]
        public ActionResult<IEnumerable<BookMarkdownInfoDto>> GetMarkdownBooks()
        {
            var booksPath = Path.Combine(_env.ContentRootPath, "Books");
            if (!Directory.Exists(booksPath))
            {
                return Ok(new List<BookMarkdownInfoDto>());
            }

            var entries = Directory.EnumerateFiles(booksPath, "*.md")
                .Select(path => new FileInfo(path))
                .OrderBy(file => file.Name)
                .Select(file => new BookMarkdownInfoDto
                {
                    FileName = file.Name,
                    Title = Path.GetFileNameWithoutExtension(file.Name),
                    LastModifiedUtc = file.LastWriteTimeUtc,
                    CoverImagePath = ExtractCoverImagePath(file)
                })
                .ToList();

            return Ok(entries);
        }

        private static string? ExtractCoverImagePath(FileInfo file)
        {
            foreach (var line in System.IO.File.ReadLines(file.FullName))
            {
                var match = CoverImageRegex.Match(line);
                if (match.Success)
                {
                    var path = match.Groups[1].Value?.Trim();
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        [HttpGet("markdown/{fileName}")]
        public async Task<ActionResult> GetMarkdownBook(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest("File name is required.");
            }

            var safeName = Path.GetFileName(fileName);
            if (!safeName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only .md files are supported.");
            }

            var booksPath = Path.Combine(_env.ContentRootPath, "Books");
            var fullPath = Path.Combine(booksPath, safeName);

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var content = await System.IO.File.ReadAllTextAsync(fullPath);
            return Content(content, "text/markdown");
        }

        [Consumes("multipart/form-data")]
        [NonAction]
        public async Task<ActionResult<Book>> PostBook([FromForm] BookUploadDto dto)
        {
            if (dto.CoverImage == null || dto.File == null)
                return BadRequest("Kép és fájl kötelező.");

            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            // kép mentése
            var coverFileName = Guid.NewGuid() + Path.GetExtension(dto.CoverImage.FileName);
            var coverPath = Path.Combine(uploads, coverFileName);
            using (var stream = new FileStream(coverPath, FileMode.Create))
            {
                await dto.CoverImage.CopyToAsync(stream);
            }

            // könyv mentése
            var bookFileName = Guid.NewGuid() + Path.GetExtension(dto.File.FileName);
            var bookPath = Path.Combine(uploads, bookFileName);
            using (var stream = new FileStream(bookPath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            var book = new Book
            {
                Title = dto.Title,
                CoverImagePath = "/uploads/" + coverFileName,
                FilePath = "/uploads/" + bookFileName
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
        }

        [NonAction]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // DTO a file feltöltéshez
    public class BookUploadDto
    {
        public string Title { get; set; } = string.Empty;
        public IFormFile CoverImage { get; set; } = null!;
        public IFormFile File { get; set; } = null!;
    }

    public class BookMarkdownInfoDto
    {
        public string FileName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime LastModifiedUtc { get; set; }
        public string? CoverImagePath { get; set; }
    }
}
