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
        // kliens: clientHash = SHA256(password + salt)
        // backend: finalHash = SHA256(clientHash) -> ezt mentjük
        // salt: külön /salt-send endpointtal jön és mentjük el
        // ----------------------------------------------------------
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromQuery] string email,
            [FromQuery] string username,
            [FromQuery] string password // <- ez itt a clientHash (SHA256(password + salt))
        )
        {
            Console.WriteLine($"REGISTER DEBUG -> email:{email} | username:{username} | clientHash:{password}");

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                return BadRequest("Email, username and password are required.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return BadRequest("Email already registered.");

            // BACKEND HASH: SHA256(clientHash)
            var finalHash = ComputeHash(password);

            var user = new User
            {
                Email = email,
                Username = username,
                PasswordHash = finalHash,
                // Salt itt még null – később /salt-send tölti ki
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully." });
        }

        // ----------------------------------------------------------
        // LOGIN
        // kliens:
        //   1) GET /auth/salt?email=...
        //   2) clientHash = SHA256(password + salt)
        //   3) POST /auth/login?email=...&password=clientHash
        //
        // backend:
        //   finalHash = SHA256(clientHash)
        //   összehasonlítja a DB-ben lévő PasswordHash-csel
        // ----------------------------------------------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromQuery] string email,
            [FromQuery] string password // <- clientHash (SHA256(password + salt))
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

            // BACKEND HASH: SHA256(clientHash)
            var finalHash = ComputeHash(password);

            if (user.PasswordHash != finalHash)
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
        // SALT LEKÉRÉSE LOGINHEZ
        //
        // Frontend:
        //   const res = await api.getSalt(email);
        //   const salt = res.data.salt;
        //   const clientHash = SHA256(password + salt);
        //   login(email, clientHash);
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
                return BadRequest("Salt not set for this user.");

            return Ok(new { email = user.Email, salt = user.Salt });
        }

        // ----------------------------------------------------------
        // SALT ELMENTÉSE REGISZTRÁCIÓ UTÁN
        //
        // Frontend reg flow:
        //   const salt = generateSalt();
        //   const clientHash = SHA256(password + salt);
        //   await register(email, username, clientHash);
        //   await api.saltSend(email, salt);
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

        // ----------------------------------------------------------
        // SHA256 HASH HELPER
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
