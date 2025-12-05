// Hubs/ChatHub.cs
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using GameApi.Data;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private static readonly Dictionary<int, string> _userConnections = new();

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                _userConnections[userId.Value] = Context.ConnectionId;
                
                // Add user to their personal group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                
                // Notify others that user came online
                await Clients.All.SendAsync("UserOnline", userId.Value);
                
                Console.WriteLine($"User {userId} connected with connection {Context.ConnectionId}");
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                _userConnections.Remove(userId.Value);
                
                // Notify others that user went offline
                await Clients.All.SendAsync("UserOffline", userId.Value);
                
                Console.WriteLine($"User {userId} disconnected");
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        // Join a channel room
        public async Task JoinChannel(int channelId)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return;

            // Verify user has access to this channel
            var hasAccess = await _context.ChannelUsers
                .AnyAsync(cu => cu.ChannelId == channelId && cu.UserId == userId.Value);
            
            if (hasAccess)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"channel_{channelId}");
                await Clients.Group($"channel_{channelId}").SendAsync("UserJoinedChannel", 
                    new { UserId = userId.Value, ChannelId = channelId });
                
                Console.WriteLine($"User {userId} joined channel {channelId}");
            }
        }

        // Leave a channel room
        public async Task LeaveChannel(int channelId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel_{channelId}");
            
            var userId = GetUserId();
            Console.WriteLine($"User {userId} left channel {channelId}");
        }

        // Send message to channel
        public async Task SendMessageToChannel(int channelId, string content)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return;

            // Save message to database
            var message = new CommunityMessage
            {
                ChannelId = channelId,
                SenderId = userId.Value,
                Content = content,
                Timestamp = DateTime.UtcNow
            };

            _context.CommunityMessages.Add(message);
            await _context.SaveChangesAsync();

            // Get sender info for the response
            var sender = await _context.Users
                .Where(u => u.Id == userId.Value)
                .Select(u => new { u.Id, u.Username })
                .FirstOrDefaultAsync();

            if (sender != null)
            {
                // Broadcast to everyone in the channel
                await Clients.Group($"channel_{channelId}").SendAsync("ReceiveMessage", new
                {
                    Id = message.Id,
                    ChannelId = channelId,
                    Sender = sender,
                    Content = content,
                    Timestamp = message.Timestamp
                });
                
                Console.WriteLine($"Message sent to channel {channelId} by user {userId}");
            }
        }

        // Typing indicators
        public async Task StartTyping(int channelId)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return;

            var user = await _context.Users
                .Where(u => u.Id == userId.Value)
                .Select(u => new { u.Id, u.Username })
                .FirstOrDefaultAsync();

            if (user != null)
            {
                await Clients.OthersInGroup($"channel_{channelId}").SendAsync("UserTyping", new
                {
                    UserId = user.Id,
                    Username = user.Username,
                    ChannelId = channelId
                });
            }
        }

        public async Task StopTyping(int channelId)
        {
            var userId = GetUserId();
            if (!userId.HasValue) return;

            await Clients.OthersInGroup($"channel_{channelId}").SendAsync("UserStoppedTyping", new
            {
                UserId = userId.Value,
                ChannelId = channelId
            });
        }

        // Get user ID from JWT token
        private int? GetUserId()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId != null ? int.Parse(userId) : null;
        }

        // Get connection ID for a specific user
        public static string? GetConnectionId(int userId)
        {
            return _userConnections.GetValueOrDefault(userId);
        }
    }
}