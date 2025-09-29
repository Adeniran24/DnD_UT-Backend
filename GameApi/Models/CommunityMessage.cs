using System;

namespace GameApi.Models
{
    public class CommunityMessage
    {
        public int Id { get; set; }
        public int ChannelId { get; set; }
        public Channel Channel { get; set; } = null!;

        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;

        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int? RecipientId { get; set; }
        public User? Recipient { get; set; }
    }
}
