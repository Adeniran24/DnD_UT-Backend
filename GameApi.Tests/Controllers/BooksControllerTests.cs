using System;
using System.IO;
using System.Linq;
using GameApi.Controllers;
using GameApi.Data;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Mvc;

namespace GameApi.Tests.Controllers;

public class BooksControllerTests
{
    [Fact]
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

    [Fact]
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
}
