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
        // REGISTER
        // ----------------------------------------------------------
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromQuery] string email,
            [FromQuery] string username,
            [FromQuery] string password)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return BadRequest("Email already registered.");

            using var sha = SHA256.Create();
            var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));

            var user = new User
            {
                Email = email,
                Username = username,
                PasswordHash = hash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully." });
        }

        // ----------------------------------------------------------
        // LOGIN + JWT TOKEN
        // ----------------------------------------------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromQuery] string email,
            [FromQuery] string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return Unauthorized("Invalid credentials.");

            using var sha = SHA256.Create();
            var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));

            if (user.PasswordHash != hash)
                return Unauthorized("Invalid credentials.");

            var token = GenerateToken(user.Id, user.Email);
            return Ok(new { token });
        }

        // ----------------------------------------------------------
        // GET CURRENT LOGGED USER DATA
        // ----------------------------------------------------------
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Username
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }

        // ----------------------------------------------------------
        // SALT ENDPOINTS (ha használod őket)
        // ----------------------------------------------------------
        [HttpGet("salt")]
        public IActionResult GetSalt([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required.");

            // Ez csak minta — ide jöhet a te salt logikád
            var salt = Guid.NewGuid().ToString("N");
            return Ok(new { email, salt });
        }

        [HttpPost("salt-send")]
        public IActionResult SaltSend(
            [FromQuery] string email,
            [FromQuery] string salt)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(salt))
                return BadRequest("Email and salt are required.");

            // Ez is csak minta — ide jöhet pl. email küldés, mentés stb.
            return Ok(new { message = "Salt accepted.", email, salt });
        }

        // ----------------------------------------------------------
        // JWT GENERATION
        // ----------------------------------------------------------
        private string GenerateToken(int userId, string email)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Email, email)
                }),
                Expires = DateTime.UtcNow.AddHours(12),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
