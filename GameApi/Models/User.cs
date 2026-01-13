using System;
using System.Collections.Generic;

namespace GameApi.Models
{
   public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? ProfilePictureUrl { get; set; } // pl: "/uploads/profile/abc.jpg"


    // 🔐 ADMIN DASHBOARDHOZ
    public string Role { get; set; } = "User"; // User | DM | Admin
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // kapcsolatok maradnak
    public ICollection<ChatRoomUser> ChatRooms { get; set; } = new List<ChatRoomUser>();
    public ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
    public ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();
    public ICollection<CommunityUser> Communities { get; set; } = new List<CommunityUser>();
    public ICollection<CommunityMessage> CommunityMessages { get; set; } = new List<CommunityMessage>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

}
