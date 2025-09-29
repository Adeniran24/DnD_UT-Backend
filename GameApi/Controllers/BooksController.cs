using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameApi.Data;
using GameApi.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BooksController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
        {
            return await _context.Books.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            return book;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
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

        [HttpDelete("{id}")]
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
}
