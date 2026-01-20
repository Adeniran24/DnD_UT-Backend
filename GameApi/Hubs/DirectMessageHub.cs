using GameApi.Data;
using GameApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameApi.Hubs
{
    [Authorize]
    public class DirectMessageHub : Hub
    {
        private readonly AppDbContext _context;

        public DirectMessageHub(AppDbContext context)
        {
            _context = context;
        }

        private int Me
        {
            get
            {
                var idStr =
                    Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    Context.User?.FindFirstValue("id") ??
                    Context.User?.FindFirstValue("userId") ??
                    Context.User?.FindFirstValue("sub");

                if (!int.TryParse(idStr, out var userId))
                {
                    throw new HubException("Missing/invalid user id claim in JWT.");
                }

                return userId;
            }
        }

        public async Task SendDm(int friendUserId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new HubException("Empty message");

            // FRIEND CHECK
            bool areFriends = await _context.Friendships.AnyAsync(f =>
                f.Status == FriendshipStatus.Accepted &&
                ((f.RequesterId == Me && f.AddresseeId == friendUserId) ||
                 (f.RequesterId == friendUserId && f.AddresseeId == Me))
            );

            if (!areFriends)
                throw new HubException("You are not friends");

            var msg = new DirectMessage
            {
                SenderId = Me,
                ReceiverId = friendUserId,
                Content = content.Trim(),
                SentAt = DateTime.UtcNow
            };

            _context.DirectMessages.Add(msg);
            await _context.SaveChangesAsync();

            var senderUsername = await _context.Users
                .Where(u => u.Id == Me)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? "Unknown";

            await Clients.Users(
                Me.ToString(),
                friendUserId.ToString()
            ).SendAsync("dmReceived", new
            {
                senderId = Me,
                receiverId = friendUserId,
                content = msg.Content,
                sentAt = msg.SentAt,
                senderUsername
            });
        }
    }
}
