using System;
using System.IO;
using System.Text;
using GameApi.Controllers;
using GameApi.Data;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameApi.Tests.Controllers;

public class PdfControllerTests
{
    private static IFormFile BuildFormFile(string name, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", name);
    }

    [Fact]
    public async Task UploadPdf_PersistsRecord()
    {
        var context = TestHelper.CreateContext(nameof(UploadPdf_PersistsRecord));
        var env = TestHelper.CreateWebHostEnvironment();
        env.ContentRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(env.ContentRootPath);

        var controller = new PdfController(env, context);
        var file = BuildFormFile("test.pdf", "pdf-content");

        var result = await controller.UploadPdf(file);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Single(context.PdfFiles);
    }

    [Fact]
    public void GetTemplatePdf_ReturnsNotFound_WhenMissing()
    {
        var context = TestHelper.CreateContext(nameof(GetTemplatePdf_ReturnsNotFound_WhenMissing));
        var env = TestHelper.CreateWebHostEnvironment();
        env.ContentRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(env.ContentRootPath);

        var controller = new PdfController(env, context);
        var result = controller.GetTemplatePdf();

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Template PDF not found.", notFound.Value);
    }
}
