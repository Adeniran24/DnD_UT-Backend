namespace GameApi.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public List<Character> Characters { get; set; } = new();
     public ICollection<ChatRoomUser> ChatRooms { get; set; } = new List<ChatRoomUser>();

        // Friend navigáció
    public ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
    public ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();
}
