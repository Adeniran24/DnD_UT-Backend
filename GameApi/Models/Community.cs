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

        public List<CommunityUser> Users { get; set; } = new();
        public List<Channel> Channels { get; set; } = new();
    }

    public class Channel
    {
        public int Id { get; set; }
        public int CommunityId { get; set; }
        public Community Community { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public ChannelType Type { get; set; } = ChannelType.Text;
        public bool IsPrivate { get; set; } = false;

        public List<CommunityMessage> Messages { get; set; } = new(); // CommunityMessage-re javítva
    }

    public enum ChannelType
    {
        Text,
        Voice,
        Category
    }

    public class CommunityUser
    {
        public int CommunityId { get; set; }
        public Community Community { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public CommunityRole Role { get; set; } = CommunityRole.Member;
    }

    public enum CommunityRole
    {
        Owner,
        Admin,
        Member
    }
}
