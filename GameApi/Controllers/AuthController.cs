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

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
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
                    u.LastLoginAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return Ok(user);
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
