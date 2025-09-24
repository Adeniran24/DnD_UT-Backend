using System.ComponentModel.DataAnnotations;

namespace GameApi.Models
{
    public class ChatRoom
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public ICollection<ChatRoomUser> Users { get; set; } = new List<ChatRoomUser>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    public class ChatRoomUser
    {
        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
