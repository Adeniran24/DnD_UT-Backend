using System.IO;
using GameApi.Data;
using GameApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
    
        public AuthController(AppDbContext context, IConfiguration config, IWebHostEnvironment env)
        {
            _context = context;
            _config = config;
            _env = env;
        }

        // ----------------------------------------------------------
        // REGISTER (NO SALT HERE – frontend sends it separately)
        // ----------------------------------------------------------
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromQuery] string email,
            [FromQuery] string username,
            [FromQuery] string password // clientHash
        )
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                return BadRequest("Missing data.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return BadRequest("Email already registered.");

            var finalHash = ComputeHash(password);

            var user = new User
            {
                Email = email,
                Username = username,
                PasswordHash = finalHash,
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // ----------------------------------------------------------
        // LOGIN
        // ----------------------------------------------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromQuery] string email,
            [FromQuery] string password // clientHash
        )
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return BadRequest("Email and password are required.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return Unauthorized("Invalid credentials.");

            if (!user.IsActive)
                return Unauthorized("User is banned.");

            var finalHash = ComputeHash(password);
            if (user.PasswordHash != finalHash)
                return Unauthorized("Invalid credentials.");

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateToken(user);

            return Ok(new { token });
        }

        // ----------------------------------------------------------
        // GET CURRENT USER
        // ----------------------------------------------------------
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Username,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt,
                    u.LastLoginAt,
                    u.ProfilePictureUrl,

                    u.ProfileThemeJson,
                    u.HasCompletedTutorial

                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpGet("me/theme")]
        [Authorize]
        public async Task<IActionResult> GetProfileTheme()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.ProfileThemeJson })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(user.ProfileThemeJson))
                return Ok(new { theme = (object?)null });

            try
            {
                var theme = JsonSerializer.Deserialize<JsonElement>(user.ProfileThemeJson);
                return Ok(new { theme });
            }
            catch
            {
                return Ok(new { theme = (object?)null });
            }
        }

        public class ProfileThemeUpdateDto
        {
            public JsonElement Theme { get; set; }
        }

        [HttpPut("me/theme")]
        [Authorize]
        public async Task<IActionResult> UpdateProfileTheme([FromBody] ProfileThemeUpdateDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found.");

            if (dto.Theme.ValueKind == JsonValueKind.Undefined || dto.Theme.ValueKind == JsonValueKind.Null)
            {
                user.ProfileThemeJson = null;
            }
            else
            {
                user.ProfileThemeJson = dto.Theme.GetRawText();
            }

            await _context.SaveChangesAsync();

            return Ok(new { theme = dto.Theme.ValueKind == JsonValueKind.Undefined ? (object?)null : dto.Theme });
        }

        public class TutorialStatusUpdateDto
        {
            public bool Completed { get; set; } = true;
        }

        [HttpPut("me/tutorial")]
        [Authorize]
        public async Task<IActionResult> UpdateTutorialStatus([FromBody] TutorialStatusUpdateDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found.");

            user.HasCompletedTutorial = dto.Completed;
            await _context.SaveChangesAsync();

            return Ok(new { hasCompletedTutorial = user.HasCompletedTutorial });
        }

        public class UpdateProfileDto
        {
            public string? Username { get; set; }
            public string? Email { get; set; }
            public string? CurrentPassword { get; set; }
            public string? NewPassword { get; set; }
        }

        public class ProfilePictureUpdateDto
        {
            public string? ProfilePicture { get; set; }
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found.");

            var nextUsername = dto.Username?.Trim();
            var nextEmail = dto.Email?.Trim();

            var wantsUsername = !string.IsNullOrWhiteSpace(nextUsername) && nextUsername != user.Username;
            var wantsEmail = !string.IsNullOrWhiteSpace(nextEmail) && nextEmail != user.Email;
            var wantsPassword = !string.IsNullOrWhiteSpace(dto.NewPassword);

            if (!wantsUsername && !wantsEmail && !wantsPassword)
                return BadRequest("No changes.");

            if (wantsEmail || wantsPassword)
            {
                if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                    return BadRequest("Current password is required.");

                var currentHash = ComputeHash(dto.CurrentPassword);
                if (currentHash != user.PasswordHash)
                    return Unauthorized("Invalid password.");
            }

            if (wantsEmail)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == nextEmail && u.Id != userId);
                if (emailExists)
                    return BadRequest("Email already registered.");
                user.Email = nextEmail!;
            }

            if (wantsUsername)
            {
                var usernameExists = await _context.Users.AnyAsync(u => u.Username == nextUsername && u.Id != userId);
                if (usernameExists)
                    return BadRequest("Username already taken.");
                user.Username = nextUsername!;
            }

            if (wantsPassword)
            {
                user.PasswordHash = ComputeHash(dto.NewPassword!);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.Email,
                user.Username,
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                user.ProfilePictureUrl,
                user.ProfileThemeJson
            });
        }

        [HttpPut("me/profile-picture")]
        [Authorize]
        [Consumes("application/json")]
        public async Task<IActionResult> UpdateProfilePictureUrl([FromBody] ProfilePictureUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProfilePicture))
                return BadRequest("Profile picture is required.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found.");

            var next = dto.ProfilePicture.Trim();
            if (!next.StartsWith("/defaults/", StringComparison.OrdinalIgnoreCase) &&
                !next.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid profile picture.");
            }

            user.ProfilePictureUrl = next;
            await _context.SaveChangesAsync();

            return Ok(new { profilePictureUrl = next });
        }
        [HttpPut("me/profile-picture")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)] // 5MB
        public async Task<IActionResult> UpdateProfilePicture([FromForm] GameApi.Models.ProfilePictureUploadDto dto)
        {
            var file = dto.File;
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExt = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExt.Contains(ext))
                return BadRequest("Invalid file type.");

            var allowedContentTypes = new HashSet<string> { "image/jpeg", "image/png", "image/webp" };
            if (!allowedContentTypes.Contains(file.ContentType))
                return BadRequest("Invalid content type.");

            var webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadDir = Path.Combine(webRoot, "uploads", "profile");
            Directory.CreateDirectory(uploadDir);

            var newFileName = $"{Guid.NewGuid():N}{ext}";
            var newPhysicalPath = Path.Combine(uploadDir, newFileName);

            var oldUrl = user.ProfilePictureUrl;

            await using (var stream = System.IO.File.Create(newPhysicalPath))
            {
                await file.CopyToAsync(stream);
            }

            var newUrl = $"/uploads/profile/{newFileName}";
            user.ProfilePictureUrl = newUrl;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(oldUrl) &&
                oldUrl.StartsWith("/uploads/profile/", StringComparison.OrdinalIgnoreCase))
            {
                var oldFileName = Path.GetFileName(oldUrl);
                var oldPhysicalPath = Path.Combine(uploadDir, oldFileName);

                if (System.IO.File.Exists(oldPhysicalPath))
                {
                    try { System.IO.File.Delete(oldPhysicalPath); } catch { }
                }
            }

            return Ok(new { profilePictureUrl = newUrl });
        }


        // ----------------------------------------------------------
        // GET SALT
        // ----------------------------------------------------------
        [HttpGet("salt")]
        public async Task<IActionResult> GetSalt([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email is required.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found.");

            if (string.IsNullOrWhiteSpace(user.Salt))
                return BadRequest("Salt not set.");

            return Ok(new { salt = user.Salt });
        }

        // ----------------------------------------------------------
        // SAVE SALT (AFTER REGISTER)
        // ----------------------------------------------------------
        [HttpPost("salt-send")]
        public async Task<IActionResult> SaltSend([FromBody] SaltSendDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return NotFound();

            user.Salt = dto.Salt;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // ----------------------------------------------------------
        // JWT GENERATION
        // ----------------------------------------------------------
        private string GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(12),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // ----------------------------------------------------------
        // HASH HELPER
        // ----------------------------------------------------------
        private string ComputeHash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
