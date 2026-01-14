using System.Security.Claims;
using GameApi.Data;
using GameApi.DTOs;
using GameApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/messages/{messageId:int}/reactions")]
    [Authorize]
    public class MessageReactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MessageReactionsController(AppDbContext context)
        {
            _context = context;
        }

        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> AddReaction(int messageId, CommunityMessageReactionCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Emoji))
            {
                return BadRequest("Emoji is required.");
            }

            var message = await _context.CommunityMessages
                .Include(m => m.Channel)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return NotFound();
            }

            var isMember = await _context.CommunityUsers
                .AnyAsync(cu => cu.CommunityId == message.Channel.CommunityId && cu.UserId == Me);

            if (!isMember)
            {
                return Forbid();
            }

            var existing = await _context.CommunityMessageReactions
                .AnyAsync(r => r.MessageId == messageId && r.UserId == Me && r.Emoji == dto.Emoji.Trim());

            if (existing)
            {
                return NoContent();
            }

            _context.CommunityMessageReactions.Add(new CommunityMessageReaction
            {
                MessageId = messageId,
                UserId = Me,
                Emoji = dto.Emoji.Trim()
            });

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{emoji}")]
        public async Task<IActionResult> RemoveReaction(int messageId, string emoji)
        {
            var reaction = await _context.CommunityMessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == Me && r.Emoji == emoji);

            if (reaction == null)
            {
                return NotFound();
            }

            _context.CommunityMessageReactions.Remove(reaction);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
