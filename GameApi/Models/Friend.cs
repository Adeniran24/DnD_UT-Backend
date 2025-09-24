using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameApi.Models
{
    public enum FriendshipStatus
    {
        None,
        Pending,
        Accepted,
        Declined,
        Blocked
    }

    public class Friendship
    {
        [Key]
        public int Id { get; set; }

        public int RequesterId { get; set; } // aki küldi a barátkérést
        [ForeignKey("RequesterId")]
        public required User Requester { get; set; }

        public int AddresseeId { get; set; } // aki kapja a kérést
        [ForeignKey("AddresseeId")]
        public required User Addressee { get; set; }

        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        // Mikor küldték a barátkérelmet
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // Mikor fogadták el (null, ha még nincs elfogadva)
        public DateTime? AcceptedAt { get; set; }

        // Ha blokkolva van, akkor itt jelezhetjük
        public bool IsBlocked => Status == FriendshipStatus.Blocked;
    }
}
