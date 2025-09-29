using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameApi.Models;
using GameApi.DTOs;
using GameApi.Data;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly AppDbContext _context;

    public MessageController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/message/channel/5
    [HttpGet("channel/{channelId}")]
    public async Task<ActionResult<IEnumerable<MessageReadDto>>> GetMessages(int channelId)
    {
        var messages = await _context.CommunityMessages // itt átírva
            .Where(m => m.ChannelId == channelId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return messages.Select(m => new MessageReadDto
        {
            Id = m.Id,
            ChannelId = m.ChannelId,
            SenderId = m.SenderId,
            Content = m.Content,
            Timestamp = m.Timestamp
        }).ToList();
    }

    // POST: api/message
    [HttpPost]
    public async Task<ActionResult<MessageReadDto>> SendMessage(MessageCreateDto dto)
    {
        var message = new CommunityMessage // itt átírva
        {
            ChannelId = dto.ChannelId,
            SenderId = dto.SenderId,
            Content = dto.Content,
            Timestamp = DateTime.UtcNow
        };

        _context.CommunityMessages.Add(message); // itt átírva
        await _context.SaveChangesAsync();

        return new MessageReadDto
        {
            Id = message.Id,
            ChannelId = message.ChannelId,
            SenderId = message.SenderId,
            Content = message.Content,
            Timestamp = message.Timestamp
        };
    }
}
