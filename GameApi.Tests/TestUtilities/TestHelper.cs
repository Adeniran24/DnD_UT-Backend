using System.Collections.Generic;
using System.Security.Claims;
using GameApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace GameApi.Tests.TestUtilities;

internal static class TestHelper
{
    public const string JwtKey = "TestingKey_32bytes_long_1234567890abcd";
    public const string JwtIssuer = "tests";
    public const string JwtAudience = "tests";

    public static IConfiguration CreateJwtConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience
            })
            .Build();

    public static AppDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new AppDbContext(options);
    }

    public static IWebHostEnvironment CreateWebHostEnvironment() =>
        new TestWebHostEnvironment
        {
            ApplicationName = "GameApi.Tests",
            EnvironmentName = "Testing",
            ContentRootPath = System.IO.Directory.GetCurrentDirectory(),
            WebRootPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot")
        };

    public static ClaimsPrincipal CreateUserPrincipal(int userId) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "Test"));

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = string.Empty;
        public IFileProvider? ContentRootFileProvider { get; set; }
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = string.Empty;
        public IFileProvider? WebRootFileProvider { get; set; }
        public string WebRootPath { get; set; } = string.Empty;
    }
}
