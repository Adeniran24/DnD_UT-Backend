using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameApi.Models;
using GameApi.DTOs;
using GameApi.Data;

[ApiController]
[Route("api/[controller]")]
public class ChannelController : ControllerBase
{
    private readonly AppDbContext _context;

    public ChannelController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/channel/5  (csak egy community csatornái)
    [HttpGet("community/{communityId}")]
    public async Task<ActionResult<IEnumerable<ChannelReadDto>>> GetChannels(int communityId)
    {
        var channels = await _context.Channels
            .Where(c => c.CommunityId == communityId)
            .ToListAsync();

        return channels.Select(c => new ChannelReadDto
        {
            Id = c.Id,
            CommunityId = c.CommunityId,
            Name = c.Name,
            Type = c.Type,
            IsPrivate = c.IsPrivate
        }).ToList();
    }

    // POST: api/channel
    [HttpPost]
    public async Task<ActionResult<ChannelReadDto>> CreateChannel(ChannelCreateDto dto)
    {
        var channel = new Channel
        {
            CommunityId = dto.CommunityId,
            Name = dto.Name,
            Type = dto.Type,
            IsPrivate = dto.IsPrivate
        };

        _context.Channels.Add(channel);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetChannels), new { communityId = dto.CommunityId }, dto);
    }
}
