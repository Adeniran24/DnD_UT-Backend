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
        public DateTime CreatedAt { get; set; }

        // Karakterek
        public List<Character> Characters { get; set; } = new();

        // Chat szobák
        public ICollection<ChatRoomUser> ChatRooms { get; set; } = new List<ChatRoomUser>();

        // Barátságok
        public ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
        public ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();

        // Community rendszer
        public ICollection<CommunityUser> Communities { get; set; } = new List<CommunityUser>();
        public ICollection<CommunityMessage> CommunityMessages { get; set; } = new List<CommunityMessage>();

        // Chat üzenetek (privát/csatorna)
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
