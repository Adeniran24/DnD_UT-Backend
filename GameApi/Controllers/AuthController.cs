using GameApi.Data;
using GameApi.Models;
using GameApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GameApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwt;

    public AuthController(AppDbContext context, JwtService jwt)
    {
        _context = context;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(string email, string username, string password)
    {
        if (await _context.Users.AnyAsync(u => u.Email == email))
            return BadRequest("Email already registered.");

        using var sha = SHA256.Create();
        var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));

        var user = new User { Email = email, Username = username, PasswordHash = hash };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User registered successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return Unauthorized("Invalid credentials.");

        using var sha = SHA256.Create();
        var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));

        if (user.PasswordHash != hash) return Unauthorized("Invalid credentials.");

        var token = _jwt.GenerateToken(user.Id, user.Email);
        return Ok(new { token });
    }
}
