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
    [Route("api/channels/{channelId:int}/messages")]
    [Authorize]
    public class ChannelMessagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChannelMessagesController(AppDbContext context)
        {
            _context = context;
        }

        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CommunityMessageDto>>> GetMessages(
            int channelId,
            [FromQuery] int? beforeId,
            [FromQuery] int limit = 50)
        {
            if (limit <= 0 || limit > 200)
            {
                limit = 50;
            }

            var channel = await _context.Channels
                .AsNoTracking()
                .FirstOrDefaultAsync(ch => ch.Id == channelId);

            if (channel == null)
            {
                return NotFound();
            }

            var isMember = await _context.CommunityUsers
                .AnyAsync(cu => cu.CommunityId == channel.CommunityId && cu.UserId == Me);

            if (!isMember)
            {
                return Forbid();
            }

            if (channel.Type == ChannelType.Voice)
            {
                return BadRequest("Voice channels do not have message history.");
            }

            var query = _context.CommunityMessages
                .Include(m => m.Sender)
                .Include(m => m.Reactions)
                .Where(m => m.ChannelId == channelId && !m.IsDeleted);

            if (beforeId.HasValue)
            {
                query = query.Where(m => m.Id < beforeId);
            }

            var messages = await query
                .OrderByDescending(m => m.Id)
                .Take(limit)
                .ToListAsync();

            var result = messages
                .OrderBy(m => m.Id)
                .Select(m => new CommunityMessageDto
                {
                    Id = m.Id,
                    ChannelId = m.ChannelId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.Username,
                    Content = m.Content,
                    Timestamp = m.Timestamp,
                    EditedAt = m.EditedAt,
                    IsPinned = m.IsPinned,
                    IsDeleted = m.IsDeleted,
                    Reactions = m.Reactions
                        .GroupBy(r => r.Emoji)
                        .Select(g => new CommunityMessageReactionDto
                        {
                            Emoji = g.Key,
                            Count = g.Count()
                        })
                        .ToList()
                })
                .ToList();

            return result;
        }

        [HttpPost]
        public async Task<ActionResult<CommunityMessageDto>> CreateMessage(int channelId, CommunityMessageCreateDto dto)
        {
            var channel = await _context.Channels
                .FirstOrDefaultAsync(ch => ch.Id == channelId);

            if (channel == null)
            {
                return NotFound();
            }

            var membership = await _context.CommunityUsers
                .FirstOrDefaultAsync(cu => cu.CommunityId == channel.CommunityId && cu.UserId == Me);

            if (membership == null)
            {
                return Forbid();
            }

            if (channel.Type == ChannelType.Voice)
            {
                return BadRequest("Voice channels do not accept messages.");
            }

            if ((channel.Type == ChannelType.News || channel.IsReadOnly) &&
                membership.Role == CommunityRole.Member)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest("Message content is required.");
            }

            var sender = await _context.Users.FindAsync(Me);

            var message = new CommunityMessage
            {
                ChannelId = channelId,
                SenderId = Me,
                Content = dto.Content.Trim(),
                Timestamp = DateTime.UtcNow
            };

            _context.CommunityMessages.Add(message);
            await _context.SaveChangesAsync();

            return new CommunityMessageDto
            {
                Id = message.Id,
                ChannelId = message.ChannelId,
                SenderId = message.SenderId,
                SenderName = sender?.Username ?? "User",
                Content = message.Content,
                Timestamp = message.Timestamp,
                EditedAt = message.EditedAt,
                IsPinned = message.IsPinned,
                IsDeleted = message.IsDeleted,
                Reactions = new List<CommunityMessageReactionDto>()
            };
        }
    }
}
