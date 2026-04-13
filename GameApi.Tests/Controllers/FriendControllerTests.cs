using System;
using System.Linq;
using GameApi.Controllers;
using GameApi.Data;
using GameApi.DTOs;
using GameApi.Models;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Tests.Controllers;

public class FriendControllerTests
{
    private static FriendController BuildController(AppDbContext context, int userId)
    {
        var controller = new FriendController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = TestHelper.CreateUserPrincipal(userId)
            }
        };
        return controller;
    }

    [Fact(DisplayName = "Get Friends Returns Accepted Friends.")]
    public async Task GetFriends_ReturnsAcceptedFriends()
    {
        var context = TestHelper.CreateContext(nameof(GetFriends_ReturnsAcceptedFriends));
        var user1 = new User { Id = 1, Username = "u1", Email = "u1@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 2, Username = "u2", Email = "u2@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(user1, user2);
        context.Friendships.Add(new Friendship
        {
            RequesterId = user1.Id,
            AddresseeId = user2.Id,
            Requester = user1,
            Addressee = user2,
            Status = FriendshipStatus.Accepted
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, user1.Id);
        var result = await controller.GetFriends();

        var ok = Assert.IsType<OkObjectResult>(result);
        var friends = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<GameApi.DTOs.FriendDto>>(ok.Value);
        Assert.Single(friends);
        Assert.Equal("u2", friends.First().Username);
    }

    [Fact(DisplayName = "Get Blocked Users Returns Blocked List.")]
    public async Task GetBlockedUsers_ReturnsBlockedList()
    {
        var context = TestHelper.CreateContext(nameof(GetBlockedUsers_ReturnsBlockedList));
        var user1 = new User { Id = 10, Username = "u10", Email = "u10@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 11, Username = "u11", Email = "u11@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(user1, user2);
        context.Friendships.Add(new Friendship
        {
            RequesterId = user1.Id,
            AddresseeId = user2.Id,
            Requester = user1,
            Addressee = user2,
            Status = FriendshipStatus.Blocked
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, user1.Id);
        var result = await controller.GetBlockedUsers();

        var ok = Assert.IsType<OkObjectResult>(result);
        var blocked = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<GameApi.DTOs.FriendDto>>(ok.Value);
        Assert.Single(blocked);
        Assert.Equal("u11", blocked.First().Username);
    }

    [Fact(DisplayName = "Search Friends excludes current user and returns matches.")]
    public async Task SearchFriends_ExcludesCurrentUser_AndReturnsMatches()
    {
        var context = TestHelper.CreateContext(nameof(SearchFriends_ExcludesCurrentUser_AndReturnsMatches));
        context.Users.AddRange(
            new User { Id = 1, Username = "hero", Email = "hero@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "heroic", Email = "heroic@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 3, Username = "mage", Email = "mage@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.SearchFriends("hero");

        var ok = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<GameApi.DTOs.FriendDto>>(ok.Value).ToList();
        Assert.Single(users);
        Assert.Equal("heroic", users[0].Username);
    }

    [Fact(DisplayName = "Get Friend Status returns none when no relation exists.")]
    public async Task GetFriendStatus_ReturnsNone_WhenMissing()
    {
        var context = TestHelper.CreateContext(nameof(GetFriendStatus_ReturnsNone_WhenMissing));
        context.Users.AddRange(
            new User { Id = 1, Username = "one", Email = "one@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "two", Email = "two@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.GetFriendStatus(2);

        var ok = Assert.IsType<OkObjectResult>(result);
        var statusProp = ok.Value!.GetType().GetProperty("Status")!.GetValue(ok.Value) as string;
        Assert.Equal("None", statusProp);
    }

    [Fact(DisplayName = "Add Friend rejects self target.")]
    public async Task AddFriend_RejectsSelfTarget()
    {
        var context = TestHelper.CreateContext(nameof(AddFriend_RejectsSelfTarget));
        context.Users.Add(new User { Id = 1, Username = "same", Email = "same@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.AddFriend("same");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("A felhasználó nem található vagy épp te vagy.", badRequest.Value);
    }

    [Fact(DisplayName = "Add Friend rejects duplicate relationship.")]
    public async Task AddFriend_RejectsDuplicateRelationship()
    {
        var context = TestHelper.CreateContext(nameof(AddFriend_RejectsDuplicateRelationship));
        var user1 = new User { Id = 1, Username = "u1", Email = "u1@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 2, Username = "u2", Email = "u2@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(user1, user2);
        context.Friendships.Add(new Friendship
        {
            RequesterId = user1.Id,
            AddresseeId = user2.Id,
            Requester = user1,
            Addressee = user2,
            Status = FriendshipStatus.Pending,
            RequestedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, user1.Id);
        var result = await controller.AddFriend(user2.Username);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Már van kapcsolat ezzel a felhasználóval.", badRequest.Value);
    }

    [Fact(DisplayName = "Add Friend creates pending request.")]
    public async Task AddFriend_CreatesPendingRequest()
    {
        var context = TestHelper.CreateContext(nameof(AddFriend_CreatesPendingRequest));
        var user1 = new User { Id = 1, Username = "u1", Email = "u1@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 2, Username = "u2", Email = "u2@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var controller = BuildController(context, user1.Id);
        var result = await controller.AddFriend(user2.Username);

        Assert.IsType<OkObjectResult>(result);
        var friendship = await context.Friendships.SingleAsync();
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
        Assert.Equal(user1.Id, friendship.RequesterId);
        Assert.Equal(user2.Id, friendship.AddresseeId);
    }

    [Fact(DisplayName = "Respond Friend Request returns not found for invalid request.")]
    public async Task RespondFriendRequest_ReturnsNotFound_ForMissingRequest()
    {
        var context = TestHelper.CreateContext(nameof(RespondFriendRequest_ReturnsNotFound_ForMissingRequest));
        var controller = BuildController(context, 99);

        var result = await controller.RespondFriendRequest(500, "accept");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Friend request nem található.", notFound.Value);
    }

    [Fact(DisplayName = "Respond Friend Request accepts pending request.")]
    public async Task RespondFriendRequest_AcceptsPendingRequest()
    {
        var context = TestHelper.CreateContext(nameof(RespondFriendRequest_AcceptsPendingRequest));
        var requester = new User { Id = 1, Username = "req", Email = "req@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var addressee = new User { Id = 2, Username = "addr", Email = "addr@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var request = new Friendship
        {
            RequesterId = requester.Id,
            AddresseeId = addressee.Id,
            Requester = requester,
            Addressee = addressee,
            Status = FriendshipStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
        context.Users.AddRange(requester, addressee);
        context.Friendships.Add(request);
        await context.SaveChangesAsync();

        var controller = BuildController(context, addressee.Id);
        var result = await controller.RespondFriendRequest(request.Id, "accept");

        Assert.IsType<OkObjectResult>(result);
        var updated = await context.Friendships
            .FirstOrDefaultAsync(f => f.Id == request.Id);
        Assert.NotNull(updated);
        Assert.Equal(FriendshipStatus.Accepted, updated!.Status);
        Assert.NotNull(updated.AcceptedAt);
    }

    [Fact(DisplayName = "Respond Friend Request rejects invalid action.")]
    public async Task RespondFriendRequest_RejectsInvalidAction()
    {
        var context = TestHelper.CreateContext(nameof(RespondFriendRequest_RejectsInvalidAction));
        var requester = new User { Id = 1, Username = "req", Email = "req@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var addressee = new User { Id = 2, Username = "addr", Email = "addr@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var request = new Friendship
        {
            RequesterId = requester.Id,
            AddresseeId = addressee.Id,
            Requester = requester,
            Addressee = addressee,
            Status = FriendshipStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
        context.Users.AddRange(requester, addressee);
        context.Friendships.Add(request);
        await context.SaveChangesAsync();

        var controller = BuildController(context, addressee.Id);
        var result = await controller.RespondFriendRequest(request.Id, "noop");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Helytelen akció.", badRequest.Value);
    }

    [Fact(DisplayName = "Get Friend Requests returns pending requests for addressee.")]
    public async Task GetFriendRequests_ReturnsPendingRequests()
    {
        var context = TestHelper.CreateContext(nameof(GetFriendRequests_ReturnsPendingRequests));
        var requester = new User { Id = 1, Username = "req", Email = "req@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var addressee = new User { Id = 2, Username = "addr", Email = "addr@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(requester, addressee);
        context.Friendships.Add(new Friendship
        {
            RequesterId = requester.Id,
            AddresseeId = addressee.Id,
            Requester = requester,
            Addressee = addressee,
            Status = FriendshipStatus.Pending,
            RequestedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, addressee.Id);
        var result = await controller.GetFriendRequests();

        var ok = Assert.IsType<OkObjectResult>(result);
        var requests = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<FriendRequestDto>>(ok.Value).ToList();
        Assert.Single(requests);
        Assert.Equal("req", requests[0].RequesterUsername);
    }

    [Fact(DisplayName = "Delete Friend returns not found when relation missing.")]
    public async Task DeleteFriend_ReturnsNotFound_WhenMissing()
    {
        var context = TestHelper.CreateContext(nameof(DeleteFriend_ReturnsNotFound_WhenMissing));
        var controller = BuildController(context, 1);

        var result = await controller.DeleteFriend(2);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Kapcsolat nem található.", notFound.Value);
    }

    [Fact(DisplayName = "Delete Friend removes existing relationship.")]
    public async Task DeleteFriend_RemovesExistingRelationship()
    {
        var context = TestHelper.CreateContext(nameof(DeleteFriend_RemovesExistingRelationship));
        var user1 = new User { Id = 1, Username = "u1", Email = "u1@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 2, Username = "u2", Email = "u2@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var friendship = new Friendship
        {
            RequesterId = user1.Id,
            AddresseeId = user2.Id,
            Requester = user1,
            Addressee = user2,
            Status = FriendshipStatus.Accepted,
            RequestedAt = DateTime.UtcNow
        };
        context.Users.AddRange(user1, user2);
        context.Friendships.Add(friendship);
        await context.SaveChangesAsync();

        var controller = BuildController(context, user1.Id);
        var result = await controller.DeleteFriend(user2.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(await context.Friendships.AnyAsync());
    }

    [Fact(DisplayName = "Block Friend creates new blocked relationship when missing.")]
    public async Task BlockFriend_CreatesBlockedRelationship_WhenMissing()
    {
        var context = TestHelper.CreateContext(nameof(BlockFriend_CreatesBlockedRelationship_WhenMissing));
        context.Users.AddRange(
            new User { Id = 1, Username = "u1", Email = "u1@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "u2", Email = "u2@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.BlockFriend(2);

        Assert.IsType<OkObjectResult>(result);
        var blocked = await context.Friendships.SingleAsync();
        Assert.Equal(FriendshipStatus.Blocked, blocked.Status);
    }

    [Fact(DisplayName = "Unblock Friend returns not found when relation missing.")]
    public async Task UnblockFriend_ReturnsNotFound_WhenMissing()
    {
        var context = TestHelper.CreateContext(nameof(UnblockFriend_ReturnsNotFound_WhenMissing));
        var controller = BuildController(context, 1);

        var result = await controller.UnblockFriend(2);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Kapcsolat nem található.", notFound.Value);
    }

    [Fact(DisplayName = "Unblock Friend removes existing relationship.")]
    public async Task UnblockFriend_RemovesExistingRelationship()
    {
        var context = TestHelper.CreateContext(nameof(UnblockFriend_RemovesExistingRelationship));
        var user1 = new User { Id = 1, Username = "u1", Email = "u1@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 2, Username = "u2", Email = "u2@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var relation = new Friendship
        {
            RequesterId = user1.Id,
            AddresseeId = user2.Id,
            Requester = user1,
            Addressee = user2,
            Status = FriendshipStatus.Blocked,
            RequestedAt = DateTime.UtcNow
        };
        context.Users.AddRange(user1, user2);
        context.Friendships.Add(relation);
        await context.SaveChangesAsync();

        var controller = BuildController(context, user1.Id);
        var result = await controller.UnblockFriend(user2.Id);

        Assert.IsType<OkObjectResult>(result);
        var updated = await context.Friendships
            .FirstOrDefaultAsync(f => f.Id == relation.Id);
        Assert.Null(updated);
    }

    [Fact(DisplayName = "Get Mutual Friends returns intersection.")]
    public async Task GetMutualFriends_ReturnsIntersection()
    {
        var context = TestHelper.CreateContext(nameof(GetMutualFriends_ReturnsIntersection));
        var user1 = new User { Id = 1, Username = "u1", Email = "u1@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 2, Username = "u2", Email = "u2@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var mutual = new User { Id = 3, Username = "mutual", Email = "u3@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var onlyMine = new User { Id = 4, Username = "mine", Email = "u4@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(user1, user2, mutual, onlyMine);
        context.Friendships.AddRange(
            new Friendship { RequesterId = user1.Id, AddresseeId = mutual.Id, Requester = user1, Addressee = mutual, Status = FriendshipStatus.Accepted, RequestedAt = DateTime.UtcNow },
            new Friendship { RequesterId = user2.Id, AddresseeId = mutual.Id, Requester = user2, Addressee = mutual, Status = FriendshipStatus.Accepted, RequestedAt = DateTime.UtcNow },
            new Friendship { RequesterId = user1.Id, AddresseeId = onlyMine.Id, Requester = user1, Addressee = onlyMine, Status = FriendshipStatus.Accepted, RequestedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var controller = BuildController(context, user1.Id);
        var result = await controller.GetMutualFriends(user2.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dtos = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<GameApi.DTOs.FriendDto>>(ok.Value).ToList();
        Assert.Single(dtos);
        Assert.Equal("mutual", dtos[0].Username);
    }

    [Fact(DisplayName = "Invite Multiple returns only existing usernames.")]
    public async Task InviteMultiple_ReturnsOnlyExistingUsers()
    {
        var context = TestHelper.CreateContext(nameof(InviteMultiple_ReturnsOnlyExistingUsers));
        context.Users.AddRange(
            new User { Id = 1, Username = "u1", Email = "u1@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "u2", Email = "u2@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.InviteMultiple("2,999");

        var ok = Assert.IsType<OkObjectResult>(result);
        var invited = ok.Value!.GetType().GetProperty("InvitedUsernames")!.GetValue(ok.Value) as System.Collections.IEnumerable;
        var names = invited!.Cast<object>().Select(v => v.ToString()).ToList();
        Assert.Single(names);
        Assert.Equal("u2", names[0]);
    }
}
