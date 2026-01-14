using System.Security.Claims;
using GameApi.Data;
using GameApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Hubs
{
    [Authorize]
    public class VoiceHub : Hub
    {
        private readonly AppDbContext _context;

        public VoiceHub(AppDbContext context)
        {
            _context = context;
        }

        private int Me => int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task JoinVoice(int channelId)
        {
            var channel = await _context.Channels
                .FirstOrDefaultAsync(ch => ch.Id == channelId);

            if (channel == null || channel.Type != ChannelType.Voice)
            {
                throw new HubException("Channel not found.");
            }

            var isMember = await _context.CommunityUsers
                .AnyAsync(cu => cu.CommunityId == channel.CommunityId && cu.UserId == Me);

            if (!isMember)
            {
                throw new HubException("Not a member.");
            }

            var state = await _context.VoiceChannelStates
                .FirstOrDefaultAsync(vs => vs.ChannelId == channelId && vs.UserId == Me);

            if (state == null)
            {
                _context.VoiceChannelStates.Add(new VoiceChannelState
                {
                    ChannelId = channelId,
                    UserId = Me,
                    JoinedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"voice:{channelId}");
            await Clients.Group($"voice:{channelId}")
                .SendAsync("voicePresence", new { channelId, userId = Me, status = "joined" });
        }

        public async Task LeaveVoice(int channelId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"voice:{channelId}");

            var state = await _context.VoiceChannelStates
                .FirstOrDefaultAsync(vs => vs.ChannelId == channelId && vs.UserId == Me);

            if (state != null)
            {
                _context.VoiceChannelStates.Remove(state);
                await _context.SaveChangesAsync();
            }

            await Clients.Group($"voice:{channelId}")
                .SendAsync("voicePresence", new { channelId, userId = Me, status = "left" });
        }

        public async Task SignalOffer(int channelId, object payload)
        {
            await Clients.OthersInGroup($"voice:{channelId}")
                .SendAsync("voiceOffer", new { fromUserId = Me, payload });
        }

        public async Task SignalAnswer(int channelId, object payload)
        {
            await Clients.OthersInGroup($"voice:{channelId}")
                .SendAsync("voiceAnswer", new { fromUserId = Me, payload });
        }

        public async Task SignalIce(int channelId, object payload)
        {
            await Clients.OthersInGroup($"voice:{channelId}")
                .SendAsync("voiceIce", new { fromUserId = Me, payload });
        }
    }
}
