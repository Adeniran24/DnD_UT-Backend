using GameApi.Data;
using GameApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameApi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminUsersController(AppDbContext context)
        {
            _context = context;
        }

        private int CurrentAdminId =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // 1️⃣ LIST USERS
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetUsers()
        {
            var users = await _context.Users
                .OrderBy(u => u.Id)
                .Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Username = u.Username,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // 2️⃣ GET SINGLE USER
        [HttpGet("{id}")]
        public async Task<ActionResult<AdminUserDto>> GetUser(int id)
        {
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Username = u.Username,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        // 3️⃣ UPDATE ROLE
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateRole(int id, UpdateUserRoleDto dto)
        {
            var validRoles = new[] { "User", "DM", "Admin" };
            if (!validRoles.Contains(dto.Role))
                return BadRequest("Invalid role");

            if (id == CurrentAdminId && dto.Role != "Admin")
                return BadRequest("You cannot change your own admin role.");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.Role = dto.Role;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 4️⃣ ACTIVATE / DEACTIVATE
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateUserStatusDto dto)
        {
            if (id == CurrentAdminId && !dto.IsActive)
                return BadRequest("You cannot deactivate your own account.");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
