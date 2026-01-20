using System.Text.Json.Serialization;

namespace GameApi.Models
{
    public enum VttRole
    {
        DM = 0,
        Player = 1
    }

    public class VttSession
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OwnerUserId { get; set; }

        [JsonIgnore]
        public User Owner { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public List<VttSessionMember> Members { get; set; } = new();

        [JsonIgnore]
        public List<VttMap> Maps { get; set; } = new();

        [JsonIgnore]
        public List<VttToken> Tokens { get; set; } = new();

        [JsonIgnore]
        public List<VttChatMessage> ChatMessages { get; set; } = new();

        [JsonIgnore]
        public List<VttAsset> Assets { get; set; } = new();
    }

    public class VttSessionMember
    {
        public int SessionId { get; set; }

        [JsonIgnore]
        public VttSession Session { get; set; } = null!;

        public int UserId { get; set; }

        [JsonIgnore]
        public User User { get; set; } = null!;

        public VttRole Role { get; set; } = VttRole.Player;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public class VttMap
    {
        public int Id { get; set; }
        public int SessionId { get; set; }

        [JsonIgnore]
        public VttSession Session { get; set; } = null!;

        public string Name { get; set; } = "Map";
        public string? ImageUrl { get; set; }

        public int GridSize { get; set; } = 50;
        public int GridOffsetX { get; set; }
        public int GridOffsetY { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class VttToken
    {
        public int Id { get; set; }
        public int SessionId { get; set; }

        [JsonIgnore]
        public VttSession Session { get; set; } = null!;

        public int? OwnerUserId { get; set; }

        [JsonIgnore]
        public User? Owner { get; set; }

        public int? CharacterId { get; set; }

        [JsonIgnore]
        public Character? Character { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        // Positions are in grid units
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 1;
        public double Height { get; set; } = 1;
        public double Rotation { get; set; }

        public bool IsHidden { get; set; }
        public bool IsLocked { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class VttChatMessage
    {
        public int Id { get; set; }
        public int SessionId { get; set; }

        [JsonIgnore]
        public VttSession Session { get; set; } = null!;

        public int UserId { get; set; }

        [JsonIgnore]
        public User User { get; set; } = null!;

        public string Type { get; set; } = "chat"; // chat | roll | system
        public string Content { get; set; } = string.Empty;
        public string? PayloadJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class VttAsset
    {
        public int Id { get; set; }
        public int SessionId { get; set; }

        [JsonIgnore]
        public VttSession Session { get; set; } = null!;

        public int UploadedByUserId { get; set; }

        [JsonIgnore]
        public User UploadedBy { get; set; } = null!;

        public string Kind { get; set; } = "asset"; // map | token | misc
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string Url { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
