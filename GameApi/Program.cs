using GameApi.Data;
using GameApi.Services;
using GameApi.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.Text;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory()
});

// ======================================================
// EXPLICIT CONFIGURATION LOADING
// ======================================================
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Debug output
Console.WriteLine("========================================");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Config File: {Directory.GetCurrentDirectory()}/appsettings.json");
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Connection String: {(string.IsNullOrEmpty(connString) ? "NOT FOUND" : "FOUND")}");
if (!string.IsNullOrEmpty(connString))
{
    // Hide password for security
    var safeConnString = connString.Length > 50 ? connString.Substring(0, 50) + "..." : connString;
    Console.WriteLine($"Connection String Preview: {safeConnString}");
}
Console.WriteLine("========================================\n");

// ======================================================
// Swagger + JWT
// ======================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GameApi",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    c.CustomSchemaIds(type => type.FullName!.Replace("+", "."));
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

// ======================================================
// SignalR
// ======================================================
builder.Services.AddSignalR();

// ======================================================
// DbContext (MySQL) - WITH VALIDATION
// ======================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("ERROR: Connection string 'DefaultConnection' is null or empty!");
    Console.WriteLine("Using fallback connection string...");

    // Fallback connection string for testing
    connectionString = "server=212.48.254.1;port=3306;database=dnddb;user=root;password='udu2y7ULY?'";
    Console.WriteLine($"Fallback: {connectionString}");
}
else
{
    Console.WriteLine($"Using configured connection string");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MariaDbServerVersion(new Version(10, 4, 32))
    )
);

// ======================================================
// JWT Authentication
// ======================================================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "fallback_key_32_chars_long_1234567890")
            )
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs/chat") ||
                     path.StartsWithSegments("/hubs/dm") ||
                     path.StartsWithSegments("/hubs/community") ||
                     path.StartsWithSegments("/hubs/voice") ||
                     path.StartsWithSegments("/hubs/vtt")))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://dnd-tool.com",
                "http://localhost:3000",
                "http://127.0.0.1:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ======================================================
// Services
// ======================================================
builder.Services.AddScoped<JwtService>();
builder.Services.AddControllers();

// ======================================================
// Proxy support (Nginx / reverse proxy)
// (Nem kötelező, de hasznos ha Request.Scheme/Host kellene)
// ======================================================
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    // Biztonságosabb: explicit proxy IP-ket megadhatsz később
    // options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
});

// ======================================================
// BUILD
// ======================================================
var app = builder.Build();

// ======================================================
// Ensure uploads folder exists
// (wwwroot/uploads - hogy ne ott dőljön el az első feltöltésnél)
// ======================================================
try
{
    var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "uploads");
    Directory.CreateDirectory(uploadsPath);
    Console.WriteLine($"Uploads directory ensured: {uploadsPath}");
}
catch (Exception ex)
{
    Console.WriteLine($"WARNING: Could not create uploads directory: {ex.Message}");
}

// ======================================================
// Middleware
// ======================================================
app.UseForwardedHeaders();
app.UseStaticFiles();
// Serve 5eTools book images from the repo-level images folder when available.
var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var bookImageRoot = string.Empty;
var bookImageCandidates = new[]
{
    Path.Combine(app.Environment.ContentRootPath, "Books", "img"),
    Path.Combine(app.Environment.ContentRootPath, "images", "5eTools-master", "5eTools-master", "img"),
    Path.Combine(app.Environment.ContentRootPath, "..", "..", "images", "5eTools-master", "5eTools-master", "img"),
    Path.Combine(webRoot, "img")
};

foreach (var candidate in bookImageCandidates)
{
    var fullPath = Path.GetFullPath(candidate);
    if (!Directory.Exists(fullPath)) continue;
    bookImageRoot = fullPath;
    break;
}

if (!string.IsNullOrWhiteSpace(bookImageRoot))
{
    var contentTypeProvider = new FileExtensionContentTypeProvider();
    contentTypeProvider.Mappings[".webp"] = "image/webp";

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(bookImageRoot),
        RequestPath = "/img",
        ContentTypeProvider = contentTypeProvider
    });

    Console.WriteLine($"Book image directory served: {bookImageRoot}");
}
else
{
    Console.WriteLine("WARNING: Book image directory not found for markdown images.");
}
app.UseRouting();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

// ======================================================
// Swagger
// ======================================================
app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        var serverUrl = $"{httpReq.Scheme}://{httpReq.Host.Value}{httpReq.PathBase}";
        swaggerDoc.Servers = new List<OpenApiServer> { new() { Url = serverUrl } };
    });
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "GameApi v1");
    c.RoutePrefix = "swagger";
});

// ======================================================
// ENDPOINTS
// ======================================================
app.MapControllers().RequireCors("CorsPolicy");
app.MapHub<DirectMessageHub>("/hubs/dm").RequireCors("CorsPolicy");
app.MapHub<CommunityHub>("/hubs/community").RequireCors("CorsPolicy");
app.MapHub<VoiceHub>("/hubs/voice").RequireCors("CorsPolicy");
app.MapHub<VttHub>("/hubs/vtt").RequireCors("CorsPolicy");

app.Run();
