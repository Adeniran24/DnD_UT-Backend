using System;
using System.Collections.Generic;

namespace GameApi.Models
{
    public class Community
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public bool IsPrivate { get; set; } = true;
        public string? CoverImage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<CommunityUser> Users { get; set; } = new();
        public List<Channel> Channels { get; set; } = new();
        public List<CommunityInvite> Invites { get; set; } = new();
    }

    public class Channel
    {
        public int Id { get; set; }
        public int CommunityId { get; set; }
        public Community Community { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public ChannelType Type { get; set; } = ChannelType.Text;
        public bool IsPrivate { get; set; } = false;
        public string? Topic { get; set; }
        public int Position { get; set; }
        public int? ParentId { get; set; }
        public Channel? Parent { get; set; }
        public List<Channel> Children { get; set; } = new();
        public bool IsArchived { get; set; } = false;
        public bool IsReadOnly { get; set; } = false;

        public List<CommunityMessage> Messages { get; set; } = new();
        public List<VoiceChannelState> VoiceStates { get; set; } = new();
    }

    public enum ChannelType
    {
        Text,
        Voice,
        Category,
        News
    }

    public class CommunityUser
    {
        public int CommunityId { get; set; }
        public Community Community { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public CommunityRole Role { get; set; } = CommunityRole.Member;
        public string? Nickname { get; set; }
    }

    public enum CommunityRole
    {
        Owner,
        Admin,
        Member
    }

    public class CommunityInvite
    {
        public int Id { get; set; }
        public int CommunityId { get; set; }
        public Community Community { get; set; } = null!;
        public string Code { get; set; } = string.Empty;
        public int CreatedById { get; set; }
        public User CreatedBy { get; set; } = null!;
        public int Uses { get; set; }
        public int? MaxUses { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class VoiceChannelState
    {
        public int ChannelId { get; set; }
        public Channel Channel { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public bool IsStreaming { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
