namespace GameApi.DTOs
{
    public class FriendDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Pending, Accepted
        public string? ProfilePictureUrl { get; set; }
    }

    public class FriendRequestDto
    {
        public int Id { get; set; }
        public string RequesterUsername { get; set; } = string.Empty;
        public string AddresseeUsername { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Pending, Accepted, Declined
    }
   

    public class ChatRoomDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Usernames { get; set; } = new List<string>();
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
    }

    public class MessageDto
{
    public string Content { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } // megfelel a modellnek
}

}


