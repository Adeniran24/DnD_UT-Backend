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

    [Fact]
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

    [Fact]
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
}
