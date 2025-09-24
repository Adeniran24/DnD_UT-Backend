using GameApi.Data;
using GameApi.DTOs;
using GameApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Chatszoba létrehozása
        [HttpPost("create-room")]
        public async Task<IActionResult> CreateRoom([FromQuery] string roomName)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var room = new ChatRoom
            {
                Name = roomName
            };
            room.Users.Add(new ChatRoomUser { UserId = userId });

            _context.ChatRooms.Add(room);
            await _context.SaveChangesAsync();

            return Ok(new { room.Id, room.Name });
        }

        // 2. Felhasználó meghívása a szobába
        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser([FromQuery] int roomId, [FromQuery] string username)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var room = await _context.ChatRooms
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null) return NotFound("Szoba nem található.");

            // Csak szobatagok hívhatnak
            if (!room.Users.Any(u => u.UserId == userId))
                return Forbid("Csak szobatagok hívhatnak.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound("Felhasználó nem található.");

            if (room.Users.Any(u => u.UserId == user.Id))
                return BadRequest("Felhasználó már a szobában van.");

            room.Users.Add(new ChatRoomUser { UserId = user.Id });
            await _context.SaveChangesAsync();

            return Ok("Felhasználó meghívva a szobába.");
        }

        // 3. Csatlakozás meglévő szobához
        [HttpPost("join")]
        public async Task<IActionResult> JoinRoom([FromQuery] int roomId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var room = await _context.ChatRooms
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null) return NotFound("Szoba nem található.");

            if (room.Users.Any(u => u.UserId == userId))
                return BadRequest("Már csatlakoztál a szobához.");

            room.Users.Add(new ChatRoomUser { UserId = userId });
            await _context.SaveChangesAsync();

            return Ok("Csatlakozás sikeres.");
        }

        // 4. Üzenet küldése szobába vagy privát
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromQuery] int? roomId, [FromQuery] string? recipientUsername, [FromBody] string content)
        {
            int senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            Message message = new Message
            {
                SenderId = senderId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            if (roomId.HasValue)
            {
                var room = await _context.ChatRooms
                    .Include(r => r.Users)
                    .FirstOrDefaultAsync(r => r.Id == roomId.Value);

                if (room == null) return NotFound("Szoba nem található.");
                if (!room.Users.Any(u => u.UserId == senderId))
                    return Forbid("Nem vagy tagja a szobának.");

                message.ChatRoomId = roomId;
            }
            else if (!string.IsNullOrEmpty(recipientUsername))
            {
                var recipient = await _context.Users.FirstOrDefaultAsync(u => u.Username == recipientUsername);
                if (recipient == null) return NotFound("Felhasználó nem található.");

                message.RecipientId = recipient.Id;
            }
            else
            {
                return BadRequest("Adj meg szobát vagy felhasználót.");
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok("Üzenet elküldve.");
        }

        // 5. Üzenetek lekérése szobából
        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages([FromQuery] int? roomId, [FromQuery] string? recipientUsername)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (roomId.HasValue)
            {
                var room = await _context.ChatRooms
                    .Include(r => r.Users)
                    .Include(r => r.Messages)
                    .ThenInclude(m => m.Sender)
                    .FirstOrDefaultAsync(r => r.Id == roomId.Value);

                if (room == null) return NotFound("Szoba nem található.");
                if (!room.Users.Any(u => u.UserId == userId))
                    return Forbid("Nem vagy tagja a szobának.");

                var messages = room.Messages.Select(m => new MessageDto
                {
                    Content = m.Content,
                    SenderUsername = m.Sender.Username,
                    CreatedAt = m.CreatedAt
                }).ToList();

                return Ok(messages);
            }
            else if (!string.IsNullOrEmpty(recipientUsername))
            {
                var recipient = await _context.Users.FirstOrDefaultAsync(u => u.Username == recipientUsername);
                if (recipient == null) return NotFound("Felhasználó nem található.");

                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => (m.SenderId == userId && m.RecipientId == recipient.Id) ||
                                (m.SenderId == recipient.Id && m.RecipientId == userId))
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new MessageDto
                    {
                        Content = m.Content,
                        SenderUsername = m.Sender.Username,
                        CreatedAt = m.CreatedAt
                    })
                    .ToListAsync();

                return Ok(messages);
            }

            return BadRequest("Adjon meg szobát vagy felhasználót.");
        }

        // 6. Kilépés a szobából
        [HttpPost("leave")]
        public async Task<IActionResult> LeaveRoom([FromQuery] int roomId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var roomUser = await _context.ChatRoomUsers
                .FirstOrDefaultAsync(ru => ru.ChatRoomId == roomId && ru.UserId == userId);

            if (roomUser == null) return NotFound("Nem vagy tagja a szobának.");

            _context.ChatRoomUsers.Remove(roomUser);
            await _context.SaveChangesAsync();

            return Ok("Kiléptél a szobából.");
        }
    }
}
