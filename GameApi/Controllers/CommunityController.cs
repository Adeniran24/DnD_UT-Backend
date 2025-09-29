using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameApi.Models;
using GameApi.DTOs;
using GameApi.Data;

[ApiController]
[Route("api/[controller]")]
public class CommunityController : ControllerBase
{
    private readonly AppDbContext _context;

    public CommunityController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/community
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommunityReadDto>>> GetCommunities()
    {
        var communities = await _context.Communities.ToListAsync();
        return communities.Select(c => new CommunityReadDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            OwnerId = c.OwnerId,
            IsPrivate = c.IsPrivate,
            CoverImage = c.CoverImage
        }).ToList();
    }

    // GET: api/community/5
    [HttpGet("{id}")]
    public async Task<ActionResult<CommunityReadDto>> GetCommunity(int id)
    {
        var c = await _context.Communities.FindAsync(id);
        if (c == null) return NotFound();

        return new CommunityReadDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            OwnerId = c.OwnerId,
            IsPrivate = c.IsPrivate,
            CoverImage = c.CoverImage
        };
    }

    // POST: api/community
    [HttpPost]
    public async Task<ActionResult<CommunityReadDto>> CreateCommunity(CommunityCreateDto dto)
    {
        var community = new Community
        {
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = dto.OwnerId,
            IsPrivate = dto.IsPrivate
        };

        _context.Communities.Add(community);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCommunity), new { id = community.Id }, dto);
    }

    // PUT: api/community/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCommunity(int id, CommunityCreateDto dto)
    {
        var community = await _context.Communities.FindAsync(id);
        if (community == null) return NotFound();

        community.Name = dto.Name;
        community.Description = dto.Description;
        community.IsPrivate = dto.IsPrivate;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/community/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCommunity(int id)
    {
        var community = await _context.Communities.FindAsync(id);
        if (community == null) return NotFound();

        _context.Communities.Remove(community);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
