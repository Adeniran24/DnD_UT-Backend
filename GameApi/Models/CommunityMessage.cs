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
        public DateTime? EditedAt { get; set; }
        public bool IsPinned { get; set; }
        public bool IsDeleted { get; set; }

        public int? RecipientId { get; set; }
        public User? Recipient { get; set; }

        public List<CommunityMessageReaction> Reactions { get; set; } = new();
    }

    public class CommunityMessageReaction
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public CommunityMessage Message { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string Emoji { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
