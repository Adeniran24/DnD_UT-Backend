using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameApi.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        public int SenderId { get; set; }
        [ForeignKey("SenderId")]
        public User Sender { get; set; } = null!;

        public int? ChatRoomId { get; set; } // null = privát üzenet
        public ChatRoom? ChatRoom { get; set; }

        public int? RecipientId { get; set; } // privát chat esetén
        [ForeignKey("RecipientId")]
        public User? Recipient { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
