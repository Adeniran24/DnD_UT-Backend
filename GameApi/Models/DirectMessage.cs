namespace GameApi.Models
{
    public class DirectMessage
    {
        public int Id { get; set; }

        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;

        public int ReceiverId { get; set; }
        public User Receiver { get; set; } = null!;

        public string Content { get; set; } = "";
        public DateTime SentAt { get; set; }
    }
}
