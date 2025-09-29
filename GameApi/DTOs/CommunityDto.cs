using System;
using System.Collections.Generic;
using GameApi.Models;

namespace GameApi.DTOs
{
    // Community DTO
    public class CommunityReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public bool IsPrivate { get; set; }
        public string? CoverImage { get; set; }
    }

    public class CommunityCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public bool IsPrivate { get; set; } = true;
    }

    // Channel DTO
    public class ChannelReadDto
    {
        public int Id { get; set; }
        public int CommunityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ChannelType Type { get; set; }
        public bool IsPrivate { get; set; }
    }

    public class ChannelCreateDto
    {
        public int CommunityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ChannelType Type { get; set; } = ChannelType.Text;
        public bool IsPrivate { get; set; } = false;
    }

    // Message DTO
    public class MessageReadDto
    {
        public int Id { get; set; }
        public int ChannelId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class MessageCreateDto
    {
        public int ChannelId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    // CommunityUser DTO
    public class CommunityUserReadDto
    {
        public int CommunityId { get; set; }
        public int UserId { get; set; }
        public CommunityRole Role { get; set; }
    }
}
