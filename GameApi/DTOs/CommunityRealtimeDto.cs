using System;
using System.Collections.Generic;
using GameApi.Models;

namespace GameApi.DTOs
{
    public class ServerCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPrivate { get; set; } = true;
        public string? CoverImage { get; set; }
    }

    public class ServerUpdateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsPrivate { get; set; }
        public string? CoverImage { get; set; }
    }

    public class ServerSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public bool IsPrivate { get; set; }
        public string? CoverImage { get; set; }
        public int MemberCount { get; set; }
    }

    public class ServerDetailDto : ServerSummaryDto
    {
        public List<ChannelDto> Channels { get; set; } = new();
    }

    public class ServerMemberDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    public class CommunityChannelCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public ChannelType Type { get; set; } = ChannelType.Text;
        public bool IsPrivate { get; set; }
        public string? Topic { get; set; }
        public int? ParentId { get; set; }
        public int Position { get; set; }
        public bool IsReadOnly { get; set; }
    }

    public class CommunityChannelUpdateDto
    {
        public string? Name { get; set; }
        public ChannelType? Type { get; set; }
        public bool? IsPrivate { get; set; }
        public string? Topic { get; set; }
        public int? ParentId { get; set; }
        public int? Position { get; set; }
        public bool? IsArchived { get; set; }
        public bool? IsReadOnly { get; set; }
    }

    public class ChannelDto
    {
        public int Id { get; set; }
        public int CommunityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ChannelType Type { get; set; }
        public bool IsPrivate { get; set; }
        public string? Topic { get; set; }
        public int? ParentId { get; set; }
        public int Position { get; set; }
        public bool IsArchived { get; set; }
        public bool IsReadOnly { get; set; }
    }

    public class CommunityMessageCreateDto
    {
        public string Content { get; set; } = string.Empty;
    }

    public class CommunityMessageDto
    {
        public int Id { get; set; }
        public int ChannelId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsPinned { get; set; }
        public bool IsDeleted { get; set; }
        public List<CommunityMessageReactionDto> Reactions { get; set; } = new();
        public List<string> MyReactions { get; set; } = new();
    }

    public class CommunityMessageReactionCreateDto
    {
        public string Emoji { get; set; } = string.Empty;
    }

    public class CommunityMessageReactionDto
    {
        public string Emoji { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class InviteCreateDto
    {
        public int? MaxUses { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class InviteDto
    {
        public string Code { get; set; } = string.Empty;
        public int CommunityId { get; set; }
        public int CreatedById { get; set; }
        public int Uses { get; set; }
        public int? MaxUses { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class VoiceStateDto
    {
        public int ChannelId { get; set; }
        public int UserId { get; set; }
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public bool IsStreaming { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
