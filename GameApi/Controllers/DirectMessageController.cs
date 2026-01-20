using GameApi.Data;
using GameApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/dm")]
    [Authorize]
    public class DirectMessageController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DirectMessageController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("with/{friendId}")]
        public async Task<IActionResult> GetHistory(int friendId)
        {
            int me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            bool areFriends = await _context.Friendships.AnyAsync(f =>
                f.Status == FriendshipStatus.Accepted &&
                ((f.RequesterId == me && f.AddresseeId == friendId) ||
                 (f.RequesterId == friendId && f.AddresseeId == me))
            );

            if (!areFriends) return Forbid();

            var messages = await _context.DirectMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.SenderId == me && m.ReceiverId == friendId) ||
                    (m.SenderId == friendId && m.ReceiverId == me)
                )
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.SenderId,
                    SenderUsername = m.Sender.Username,
                    m.ReceiverId,
                    ReceiverUsername = m.Receiver.Username,
                    m.Content,
                    m.SentAt
                })
                .ToListAsync();

            return Ok(messages);
        }
    }
}
