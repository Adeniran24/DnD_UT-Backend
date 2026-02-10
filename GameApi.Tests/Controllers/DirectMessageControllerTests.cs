using System;
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

    [Fact]
    public async Task GetHistory_Forbids_WhenNotFriends()
    {
        var context = TestHelper.CreateContext(nameof(GetHistory_Forbids_WhenNotFriends));
        var controller = BuildController(context, 1);

        var result = await controller.GetHistory(2);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
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
}
