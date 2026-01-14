using System.Security.Claims;
using GameApi.Data;
using GameApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Hubs
{
    [Authorize]
    public class CommunityHub : Hub
    {
        private readonly AppDbContext _context;

        public CommunityHub(AppDbContext context)
        {
            _context = context;
        }

        private int Me => int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task JoinChannel(int channelId)
        {
            var channel = await _context.Channels
                .FirstOrDefaultAsync(ch => ch.Id == channelId);

            if (channel == null)
            {
                throw new HubException("Channel not found.");
            }

            var isMember = await _context.CommunityUsers
                .AnyAsync(cu => cu.CommunityId == channel.CommunityId && cu.UserId == Me);

            if (!isMember)
            {
                throw new HubException("Not a member.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}");
        }

        public async Task LeaveChannel(int channelId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");
        }

        public async Task SendMessage(int channelId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new HubException("Empty message.");
            }

            var channel = await _context.Channels
                .FirstOrDefaultAsync(ch => ch.Id == channelId);

            if (channel == null || channel.Type == ChannelType.Voice)
            {
                throw new HubException("Invalid channel.");
            }

            var membership = await _context.CommunityUsers
                .FirstOrDefaultAsync(cu => cu.CommunityId == channel.CommunityId && cu.UserId == Me);

            if (membership == null)
            {
                throw new HubException("Not a member.");
            }

            if ((channel.Type == ChannelType.News || channel.IsReadOnly) &&
                membership.Role == CommunityRole.Member)
            {
                throw new HubException("Read-only channel.");
            }

            var sender = await _context.Users.FindAsync(Me);

            var message = new CommunityMessage
            {
                ChannelId = channelId,
                SenderId = Me,
                Content = content.Trim(),
                Timestamp = DateTime.UtcNow
            };

            _context.CommunityMessages.Add(message);
            await _context.SaveChangesAsync();

            await Clients.Group($"channel:{channelId}").SendAsync("messageReceived", new
            {
                id = message.Id,
                channelId = message.ChannelId,
                senderId = message.SenderId,
                senderName = sender?.Username ?? "User",
                content = message.Content,
                timestamp = message.Timestamp
            });
        }

        public async Task Typing(int channelId)
        {
            await Clients.OthersInGroup($"channel:{channelId}")
                .SendAsync("typing", new { channelId, userId = Me });
        }
    }
}
