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

            var message = new CommunityMessage
            {
                SenderId = senderId,
                Content = content,
                Timestamp = DateTime.UtcNow
            };

            if (roomId.HasValue)
            {
                var room = await _context.ChatRooms
                    .Include(r => r.Users)
                    .FirstOrDefaultAsync(r => r.Id == roomId.Value);

                if (room == null) return NotFound("Szoba nem található.");
                if (!room.Users.Any(u => u.UserId == senderId))
                    return Forbid("Nem vagy tagja a szobának.");

                message.ChannelId = roomId.Value; // Feltételezve, hogy minden szoba egy channel
            }
            else if (!string.IsNullOrEmpty(recipientUsername))
            {
                var recipient = await _context.Users.FirstOrDefaultAsync(u => u.Username == recipientUsername);
                if (recipient == null) return NotFound("Felhasználó nem található.");

                // Itt ha privát üzenetet akarsz, érdemes külön mezőt létrehozni a CommunityMessage-ben, pl. RecipientId
                return BadRequest("Privát üzenetek még nem támogatottak ebben a verzióban.");
            }
            else
            {
                return BadRequest("Adj meg szobát vagy felhasználót.");
            }

            _context.CommunityMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok("Üzenet elküldve.");
        }

        // 5. Üzenetek lekérése szobából
        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages([FromQuery] int channelId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var channelMessages = await _context.CommunityMessages
                .Include(m => m.Sender)
                .Where(m => m.ChannelId == channelId)
                .OrderBy(m => m.Timestamp)
                .Select(m => new MessageDto
                {
                    Content = m.Content,
                    SenderUsername = m.Sender.Username,
                    CreatedAt = m.Timestamp
                })
                .ToListAsync();

            return Ok(channelMessages);
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
