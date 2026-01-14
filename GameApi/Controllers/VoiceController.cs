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
    [Route("api/voice")]
    [Authorize]
    public class VoiceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VoiceController(AppDbContext context)
        {
            _context = context;
        }

        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("{channelId:int}/join")]
        public async Task<ActionResult<VoiceStateDto>> JoinVoice(int channelId)
        {
            var channel = await _context.Channels
                .FirstOrDefaultAsync(ch => ch.Id == channelId);

            if (channel == null || channel.Type != ChannelType.Voice)
            {
                return BadRequest("Channel is not a voice channel.");
            }

            var isMember = await _context.CommunityUsers
                .AnyAsync(cu => cu.CommunityId == channel.CommunityId && cu.UserId == Me);

            if (!isMember)
            {
                return Forbid();
            }

            var state = await _context.VoiceChannelStates
                .FirstOrDefaultAsync(vs => vs.ChannelId == channelId && vs.UserId == Me);

            if (state == null)
            {
                state = new VoiceChannelState
                {
                    ChannelId = channelId,
                    UserId = Me,
                    JoinedAt = DateTime.UtcNow
                };
                _context.VoiceChannelStates.Add(state);
                await _context.SaveChangesAsync();
            }

            return new VoiceStateDto
            {
                ChannelId = state.ChannelId,
                UserId = state.UserId,
                IsMuted = state.IsMuted,
                IsDeafened = state.IsDeafened,
                IsStreaming = state.IsStreaming,
                JoinedAt = state.JoinedAt
            };
        }

        [HttpPost("{channelId:int}/leave")]
        public async Task<IActionResult> LeaveVoice(int channelId)
        {
            var state = await _context.VoiceChannelStates
                .FirstOrDefaultAsync(vs => vs.ChannelId == channelId && vs.UserId == Me);

            if (state == null)
            {
                return NoContent();
            }

            _context.VoiceChannelStates.Remove(state);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{channelId:int}/state")]
        public async Task<IActionResult> UpdateState(int channelId, VoiceStateDto dto)
        {
            var state = await _context.VoiceChannelStates
                .FirstOrDefaultAsync(vs => vs.ChannelId == channelId && vs.UserId == Me);

            if (state == null)
            {
                return NotFound();
            }

            state.IsMuted = dto.IsMuted;
            state.IsDeafened = dto.IsDeafened;
            state.IsStreaming = dto.IsStreaming;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
