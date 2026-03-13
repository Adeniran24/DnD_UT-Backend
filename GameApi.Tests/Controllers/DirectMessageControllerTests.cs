using System;
using System.Linq;
using GameApi.Controllers;
using GameApi.Data;
using GameApi.Models;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Tests.Controllers;

public class DirectMessageControllerTests
{
    private static DirectMessageController BuildController(AppDbContext context, int userId)
    {
        var controller = new DirectMessageController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = TestHelper.CreateUserPrincipal(userId)
            }
        };
        return controller;
    }

    [Fact(DisplayName = "Get History returns unauthorized when identity claim is missing.")]
    public async Task GetHistory_ReturnsUnauthorized_WhenClaimMissing()
    {
        var context = TestHelper.CreateContext(nameof(GetHistory_ReturnsUnauthorized_WhenClaimMissing));
        var controller = new DirectMessageController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetHistory(2);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact(DisplayName = "Get History Forbids When Not Friends.")]
    public async Task GetHistory_Forbids_WhenNotFriends()
    {
        var context = TestHelper.CreateContext(nameof(GetHistory_Forbids_WhenNotFriends));
        var controller = BuildController(context, 1);

        var result = await controller.GetHistory(2);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact(DisplayName = "Get History Returns Messages When Friends.")]
    public async Task GetHistory_ReturnsMessages_WhenFriends()
    {
        var context = TestHelper.CreateContext(nameof(GetHistory_ReturnsMessages_WhenFriends));
        var user1 = new User { Id = 1, Username = "a", Email = "a@a.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 2, Username = "b", Email = "b@b.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(user1, user2);
        context.Friendships.Add(new Friendship
        {
            RequesterId = user1.Id,
            AddresseeId = user2.Id,
            Requester = user1,
            Addressee = user2,
            Status = FriendshipStatus.Accepted
        });
        context.DirectMessages.Add(new DirectMessage
        {
            SenderId = user1.Id,
            ReceiverId = user2.Id,
            Content = "hi",
            SentAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, user1.Id);
        var result = await controller.GetHistory(user2.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var messages = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<object>>(ok.Value);
        Assert.Single(messages);
    }

    [Fact(DisplayName = "Get History returns both directions in chronological order.")]
    public async Task GetHistory_ReturnsBothDirectionsInOrder()
    {
        var context = TestHelper.CreateContext(nameof(GetHistory_ReturnsBothDirectionsInOrder));
        var me = new User { Id = 1, Username = "me", Email = "me@a.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var friend = new User { Id = 2, Username = "friend", Email = "friend@a.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(me, friend);
        context.Friendships.Add(new Friendship
        {
            RequesterId = me.Id,
            AddresseeId = friend.Id,
            Requester = me,
            Addressee = friend,
            Status = FriendshipStatus.Accepted
        });
        context.DirectMessages.AddRange(
            new DirectMessage { SenderId = me.Id, ReceiverId = friend.Id, Content = "first", SentAt = DateTime.UtcNow.AddMinutes(-2) },
            new DirectMessage { SenderId = friend.Id, ReceiverId = me.Id, Content = "second", SentAt = DateTime.UtcNow.AddMinutes(-1) }
        );
        await context.SaveChangesAsync();

        var controller = BuildController(context, me.Id);
        var result = await controller.GetHistory(friend.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var messages = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value).Cast<object>().ToList();
        Assert.Equal(2, messages.Count);
        var firstContent = messages[0].GetType().GetProperty("Content")!.GetValue(messages[0]) as string;
        var secondContent = messages[1].GetType().GetProperty("Content")!.GetValue(messages[1]) as string;
        Assert.Equal("first", firstContent);
        Assert.Equal("second", secondContent);
    }
}
