using GameApi.Data;
using GameApi.DTOs;
using GameApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FriendController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Friend list lekérése
        [HttpGet("list")]
        public async Task<IActionResult> GetFriends()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var friends = await _context.Friendships
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted)
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .ToListAsync();

            var friendDtos = friends.Select(f =>
            {
                var friendUser = f.RequesterId == userId ? f.Addressee : f.Requester;
                return new FriendDto
                {
                    Id = friendUser?.Id ?? 0,
                    Username = friendUser?.Username ?? string.Empty,
                    Status = f.Status.ToString(),
                    ProfilePictureUrl = friendUser?.ProfilePictureUrl
                };
            }).ToList();

            return Ok(friendDtos);
        }

        // 2. Friend keresése / ajánlás
        [HttpGet("search")]
        public async Task<IActionResult> SearchFriends([FromQuery] string query)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var users = await _context.Users
                .Where(u => u.Username.Contains(query) && u.Id != userId)
                .ToListAsync();

            var dtos = users.Select(u => new FriendDto
            {
                Id = u.Id,
                Username = u.Username,
                Status = "None",
                ProfilePictureUrl = u.ProfilePictureUrl
            }).ToList();

            return Ok(dtos);
        }

        // 3. Friend státusz lekérése
        [HttpGet("status/{targetUserId}")]
        public async Task<IActionResult> GetFriendStatus(int targetUserId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == userId && f.AddresseeId == targetUserId) ||
                    (f.RequesterId == targetUserId && f.AddresseeId == userId));

            string status = friendship != null ? friendship.Status.ToString() : "None";
            return Ok(new { Status = status });
        }

        // 4. Friend request küldése
        [HttpPost("add")]
        public async Task<IActionResult> AddFriend([FromQuery] string username)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var requesterUser = await _context.Users.FindAsync(userId);

            if (targetUser == null || targetUser.Id == userId || requesterUser == null)
                return BadRequest("A felhasználó nem található vagy épp te vagy.");

            var existing = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.RequesterId == userId && f.AddresseeId == targetUser.Id) ||
                                          (f.RequesterId == targetUser.Id && f.AddresseeId == userId));
            if (existing != null)
                return BadRequest("Már van kapcsolat ezzel a felhasználóval.");

            var friendship = new Friendship
            {
                RequesterId = userId,
                AddresseeId = targetUser.Id,
                Requester = requesterUser,
                Addressee = targetUser,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Friend request elküldve!" });
        }

        // 5. Friend request elfogadása / elutasítása
        [HttpPost("respond")]
        public async Task<IActionResult> RespondFriendRequest([FromQuery] int requestId, [FromQuery] string action)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var request = await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .FirstOrDefaultAsync(f => f.Id == requestId && f.AddresseeId == userId && f.Status == FriendshipStatus.Pending);

            if (request == null)
                return NotFound("Friend request nem található.");

            if (action.ToLower() == "accept")
            {
                request.Status = FriendshipStatus.Accepted;
                request.AcceptedAt = DateTime.UtcNow;
            }
            else if (action.ToLower() == "decline")
                request.Status = FriendshipStatus.Declined;
            else
                return BadRequest("Helytelen akció.");

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Friend request {request.Status.ToString().ToLower()}." });
        }

        // 6. Friend request-ek listázása
        [HttpGet("requests")]
        public async Task<IActionResult> GetFriendRequests()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var requests = await _context.Friendships
                .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .ToListAsync();

            var requestDtos = requests.Select(f => new FriendRequestDto
            {
                Id = f.Id,
                RequesterUsername = f.Requester!.Username,
                AddresseeUsername = f.Addressee!.Username,
                Status = f.Status.ToString()
            }).ToList();

            return Ok(requestDtos);
        }

        // 7. Barát törlése
        [HttpDelete("{friendId}")]
        public async Task<IActionResult> DeleteFriend(int friendId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.RequesterId == userId && f.AddresseeId == friendId) ||
                                          (f.RequesterId == friendId && f.AddresseeId == userId));

            if (friendship == null)
                return NotFound("Kapcsolat nem található.");

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Barát törölve." });
        }

        // 8. Barát blokkolása / státusz frissítése
        [HttpPost("block")]
        public async Task<IActionResult> BlockFriend([FromQuery] int userIdToBlock)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var requesterUser = await _context.Users.FindAsync(userId);
            var addresseeUser = await _context.Users.FindAsync(userIdToBlock);

            if (requesterUser == null || addresseeUser == null)
                return BadRequest("Felhasználó nem található.");

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.RequesterId == userId && f.AddresseeId == userIdToBlock) ||
                                          (f.RequesterId == userIdToBlock && f.AddresseeId == userId));

            if (friendship == null)
            {
                friendship = new Friendship
                {
                    RequesterId = userId,
                    AddresseeId = userIdToBlock,
                    Requester = requesterUser,
                    Addressee = addresseeUser,
                    Status = FriendshipStatus.Blocked,
                    RequestedAt = DateTime.UtcNow
                };
                _context.Friendships.Add(friendship);
            }
            else
            {
                friendship.Status = FriendshipStatus.Blocked;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Felhasználó blokkolva." });
        }

        // Felhasználó unblock-olása
[HttpPost("unblock")]
public async Task<IActionResult> UnblockFriend([FromQuery] int userIdToUnblock)
{
    int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var friendship = await _context.Friendships
        .FirstOrDefaultAsync(f => (f.RequesterId == userId && f.AddresseeId == userIdToUnblock) ||
                                  (f.RequesterId == userIdToUnblock && f.AddresseeId == userId));

    if (friendship == null)
        return NotFound("Kapcsolat nem található.");

    // Ha blokk volt, visszaállítjuk Pending/None-ra
    friendship.Status = FriendshipStatus.None; // vagy Accepted, ha korábban barátok voltak

    await _context.SaveChangesAsync();
    return Ok(new { message = "Felhasználó unblock-olva." });
}
        // 9. Közös barátok listázása
        [HttpGet("mutual/{otherUserId}")]
        public async Task<IActionResult> GetMutualFriends(int otherUserId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var myFriends = await _context.Friendships
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted)
                .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
                .ToListAsync();

            var otherFriends = await _context.Friendships
                .Where(f => (f.RequesterId == otherUserId || f.AddresseeId == otherUserId) && f.Status == FriendshipStatus.Accepted)
                .Select(f => f.RequesterId == otherUserId ? f.AddresseeId : f.RequesterId)
                .ToListAsync();

            var mutualIds = myFriends.Intersect(otherFriends);

            var mutualUsers = await _context.Users
                .Where(u => mutualIds.Contains(u.Id))
                .ToListAsync();

            var dtos = mutualUsers.Select(u => new FriendDto
            {
                Id = u.Id,
                Username = u.Username,
                Status = "Accepted"
            }).ToList();

            return Ok(dtos);
        }

        // 10. Online barátok lekérdezése (placeholder)
        [HttpGet("online")]
        public async Task<IActionResult> GetOnlineFriends()
        {
            // TODO: Integrálni SignalR-rel vagy más real-time rendszerrel
            return Ok(new { OnlineFriends = new List<string>() });
        }

        // 11. Értesítések (placeholder)
        [HttpGet("notifications")]
        public async Task<IActionResult> GetFriendNotifications()
        {
            // TODO: Push / toast értesítés
            return Ok(new { Notifications = new List<string>() });
        }

        // 12. Csoportos barát meghívás (pl. szobához)
        [HttpPost("invite-multiple")]
        public async Task<IActionResult> InviteMultiple([FromQuery] string userIds)
        {
            var ids = userIds.Split(',').Select(int.Parse).ToList();
            var existingUsers = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();

            // Itt lehet logikát írni pl. chat room meghívásra
            return Ok(new { InvitedUsernames = existingUsers.Select(u => u.Username).ToList() });
        }
    }
}
