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
    [Route("api")]
    [Authorize]
    public class InvitesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InvitesController(AppDbContext context)
        {
            _context = context;
        }

        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("servers/{id:int}/invites")]
        public async Task<ActionResult<InviteDto>> CreateInvite(int id, InviteCreateDto dto)
        {
            var membership = await _context.CommunityUsers
                .FirstOrDefaultAsync(cu => cu.CommunityId == id && cu.UserId == Me);

            if (membership == null || membership.Role == CommunityRole.Member)
            {
                return Forbid();
            }

            var code = Guid.NewGuid().ToString("N")[..8];

            var invite = new CommunityInvite
            {
                CommunityId = id,
                Code = code,
                CreatedById = Me,
                MaxUses = dto.MaxUses,
                ExpiresAt = dto.ExpiresAt
            };

            _context.CommunityInvites.Add(invite);
            await _context.SaveChangesAsync();

            return new InviteDto
            {
                Code = invite.Code,
                CommunityId = invite.CommunityId,
                CreatedById = invite.CreatedById,
                Uses = invite.Uses,
                MaxUses = invite.MaxUses,
                ExpiresAt = invite.ExpiresAt,
                CreatedAt = invite.CreatedAt
            };
        }

        [HttpGet("servers/{id:int}/invites")]
        public async Task<ActionResult<IEnumerable<InviteDto>>> GetInvites(int id)
        {
            var membership = await _context.CommunityUsers
                .FirstOrDefaultAsync(cu => cu.CommunityId == id && cu.UserId == Me);

            if (membership == null || membership.Role == CommunityRole.Member)
            {
                return Forbid();
            }

            var invites = await _context.CommunityInvites
                .Where(i => i.CommunityId == id)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new InviteDto
                {
                    Code = i.Code,
                    CommunityId = i.CommunityId,
                    CreatedById = i.CreatedById,
                    Uses = i.Uses,
                    MaxUses = i.MaxUses,
                    ExpiresAt = i.ExpiresAt,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            return invites;
        }

        [HttpPost("invites/{code}/join")]
        public async Task<IActionResult> JoinInvite(string code)
        {
            var invite = await _context.CommunityInvites
                .FirstOrDefaultAsync(i => i.Code == code);

            if (invite == null)
            {
                return NotFound();
            }

            if (invite.ExpiresAt.HasValue && invite.ExpiresAt.Value < DateTime.UtcNow)
            {
                return BadRequest("Invite expired.");
            }

            if (invite.MaxUses.HasValue && invite.Uses >= invite.MaxUses.Value)
            {
                return BadRequest("Invite has reached max uses.");
            }

            var alreadyMember = await _context.CommunityUsers
                .AnyAsync(cu => cu.CommunityId == invite.CommunityId && cu.UserId == Me);

            if (alreadyMember)
            {
                return NoContent();
            }

            _context.CommunityUsers.Add(new CommunityUser
            {
                CommunityId = invite.CommunityId,
                UserId = Me,
                Role = CommunityRole.Member
            });

            invite.Uses += 1;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
