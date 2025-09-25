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
    public async Task<ActionResult<Book>> PostBook([FromForm] string title, [FromForm] IFormFile coverImage, [FromForm] IFormFile file)
    {
        if (coverImage == null || file == null)
            return BadRequest("Kép és fájl kötelező.");

        var uploads = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(uploads))
            Directory.CreateDirectory(uploads);

        // kép mentése
        var coverFileName = Guid.NewGuid() + Path.GetExtension(coverImage.FileName);
        var coverPath = Path.Combine(uploads, coverFileName);
        using (var stream = new FileStream(coverPath, FileMode.Create))
        {
            await coverImage.CopyToAsync(stream);
        }

        // könyv mentése
        var bookFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var bookPath = Path.Combine(uploads, bookFileName);
        using (var stream = new FileStream(bookPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var book = new Book
        {
            Title = title,
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
