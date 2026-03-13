using System;
using System.IO;
using System.Linq;
using System.Text;
using GameApi.Controllers;
using GameApi.Data;
using GameApi.Models;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Tests.Controllers;

public class BooksControllerTests
{
    private static IFormFile BuildFormFile(string name, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", name);
    }

    [Fact(DisplayName = "Get Books returns items from database.")]
    public async Task GetBooks_ReturnsItemsFromDatabase()
    {
        var context = TestHelper.CreateContext(nameof(GetBooks_ReturnsItemsFromDatabase));
        context.Books.AddRange(
            new Book { Title = "One", CoverImagePath = "/uploads/one.png", FilePath = "/uploads/one.pdf" },
            new Book { Title = "Two", CoverImagePath = "/uploads/two.png", FilePath = "/uploads/two.pdf" }
        );
        await context.SaveChangesAsync();

        var controller = new BooksController(context, TestHelper.CreateWebHostEnvironment());
        var result = await controller.GetBooks();

        var books = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Book>>(result.Value);
        Assert.Equal(2, books.Count());
    }

    [Fact(DisplayName = "Get Book returns not found for unknown id.")]
    public async Task GetBook_ReturnsNotFound_ForUnknownId()
    {
        var context = TestHelper.CreateContext(nameof(GetBook_ReturnsNotFound_ForUnknownId));
        var controller = new BooksController(context, TestHelper.CreateWebHostEnvironment());

        var result = await controller.GetBook(404);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact(DisplayName = "Get Book returns entity for existing id.")]
    public async Task GetBook_ReturnsEntity_ForExistingId()
    {
        var context = TestHelper.CreateContext(nameof(GetBook_ReturnsEntity_ForExistingId));
        var book = new Book { Title = "Guide", CoverImagePath = "/uploads/c.png", FilePath = "/uploads/b.pdf" };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var controller = new BooksController(context, TestHelper.CreateWebHostEnvironment());
        var result = await controller.GetBook(book.Id);

        var returned = Assert.IsType<Book>(result.Value);
        Assert.Equal(book.Title, returned.Title);
    }

    [Fact(DisplayName = "Get Markdown Books Returns Empty When Folder Missing.")]
    public void GetMarkdownBooks_ReturnsEmpty_WhenFolderMissing()
    {
        var context = TestHelper.CreateContext(nameof(GetMarkdownBooks_ReturnsEmpty_WhenFolderMissing));
        var env = TestHelper.CreateWebHostEnvironment();
        env.ContentRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var controller = new BooksController(context, env);
        var result = controller.GetMarkdownBooks();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<BookMarkdownInfoDto>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact(DisplayName = "Get Markdown Book Returns Not Found When File Missing.")]
    public async Task GetMarkdownBook_ReturnsNotFound_WhenFileMissing()
    {
        var context = TestHelper.CreateContext(nameof(GetMarkdownBook_ReturnsNotFound_WhenFileMissing));
        var env = TestHelper.CreateWebHostEnvironment();
        env.ContentRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Books"));

        var controller = new BooksController(context, env);
        var result = await controller.GetMarkdownBook("missing.md");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact(DisplayName = "Get Markdown Book rejects empty file name.")]
    public async Task GetMarkdownBook_RejectsEmptyFileName()
    {
        var context = TestHelper.CreateContext(nameof(GetMarkdownBook_RejectsEmptyFileName));
        var controller = new BooksController(context, TestHelper.CreateWebHostEnvironment());

        var result = await controller.GetMarkdownBook(string.Empty);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("File name is required.", badRequest.Value);
    }

    [Fact(DisplayName = "Get Markdown Book rejects non-md extension.")]
    public async Task GetMarkdownBook_RejectsNonMdExtension()
    {
        var context = TestHelper.CreateContext(nameof(GetMarkdownBook_RejectsNonMdExtension));
        var controller = new BooksController(context, TestHelper.CreateWebHostEnvironment());

        var result = await controller.GetMarkdownBook("notes.txt");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Only .md files are supported.", badRequest.Value);
    }

    [Fact(DisplayName = "Get Markdown Book returns markdown content when file exists.")]
    public async Task GetMarkdownBook_ReturnsContent_WhenFileExists()
    {
        var context = TestHelper.CreateContext(nameof(GetMarkdownBook_ReturnsContent_WhenFileExists));
        var env = TestHelper.CreateWebHostEnvironment();
        env.ContentRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Books"));
        var filePath = Path.Combine(env.ContentRootPath, "Books", "guide.md");
        await File.WriteAllTextAsync(filePath, "# Hello");

        var controller = new BooksController(context, env);
        var result = await controller.GetMarkdownBook("guide.md");

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/markdown", content.ContentType);
        Assert.Equal("# Hello", content.Content);
    }

    [Fact(DisplayName = "Delete Book returns not found for missing id.")]
    public async Task DeleteBook_ReturnsNotFound_ForMissingId()
    {
        var context = TestHelper.CreateContext(nameof(DeleteBook_ReturnsNotFound_ForMissingId));
        var controller = new BooksController(context, TestHelper.CreateWebHostEnvironment());

        var result = await controller.DeleteBook(777);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact(DisplayName = "Delete Book removes row for existing id.")]
    public async Task DeleteBook_RemovesRow_ForExistingId()
    {
        var context = TestHelper.CreateContext(nameof(DeleteBook_RemovesRow_ForExistingId));
        var book = new Book { Title = "DeleteMe", CoverImagePath = "/uploads/c.png", FilePath = "/uploads/f.pdf" };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var controller = new BooksController(context, TestHelper.CreateWebHostEnvironment());
        var result = await controller.DeleteBook(book.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await context.Books.AnyAsync());
    }

    [Fact(DisplayName = "Post Book persists files and metadata.")]
    public async Task PostBook_PersistsFilesAndMetadata()
    {
        var context = TestHelper.CreateContext(nameof(PostBook_PersistsFilesAndMetadata));
        var env = TestHelper.CreateWebHostEnvironment();
        env.WebRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(env.WebRootPath);

        var controller = new BooksController(context, env);
        var dto = new BookUploadDto
        {
            Title = "New Book",
            CoverImage = BuildFormFile("cover.png", "img"),
            File = BuildFormFile("book.pdf", "pdf")
        };

        var result = await controller.PostBook(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var saved = Assert.IsType<Book>(created.Value);
        Assert.Equal("New Book", saved.Title);
        Assert.StartsWith("/uploads/", saved.CoverImagePath);
        Assert.StartsWith("/uploads/", saved.FilePath);
        Assert.True(await context.Books.AnyAsync());
    }
}
