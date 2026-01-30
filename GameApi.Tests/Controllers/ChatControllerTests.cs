using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameApi.Controllers;
using GameApi.Data;
using GameApi.DTOs;
using GameApi.Models;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Tests.Controllers;

public class ChatControllerTests
{
    private static ChatController BuildController(AppDbContext context, int userId)
    {
        var controller = new ChatController(context);
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
    public async Task CreateRoom_CreatesRoomAndAddsCreator()
    {
        var context = TestHelper.CreateContext(nameof(CreateRoom_CreatesRoomAndAddsCreator));
        var controller = BuildController(context, 42);

        var result = await controller.CreateRoom("Strategy Room");

        var ok = Assert.IsType<OkObjectResult>(result);
        var room = await context.ChatRooms.Include(r => r.Users).SingleAsync();
        Assert.Equal("Strategy Room", room.Name);
        Assert.Contains(room.Users, user => user.UserId == 42);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task InviteUser_ForbidsNonMember()
    {
        var context = TestHelper.CreateContext(nameof(InviteUser_ForbidsNonMember));
        var user1 = new User { Id = 1, Username = "alice", Email = "alice@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 2, Username = "bob", Email = "bob@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var room = new ChatRoom();
        room.Users.Add(new ChatRoomUser { UserId = user2.Id });

        context.Users.AddRange(user1, user2);
        context.ChatRooms.Add(room);
        await context.SaveChangesAsync();

        var controller = BuildController(context, user1.Id);
        var result = await controller.InviteUser(room.Id, user2.Username);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task InviteUser_AddsUserWhenInviterIsMember()
    {
        var context = TestHelper.CreateContext(nameof(InviteUser_AddsUserWhenInviterIsMember));
        var creator = new User { Username = "creator", Email = "creator@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var guest = new User { Username = "guest", Email = "guest@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var room = new ChatRoom();
        room.Users.Add(new ChatRoomUser { User = creator });

        context.Users.AddRange(creator, guest);
        context.ChatRooms.Add(room);
        await context.SaveChangesAsync();

        var controller = BuildController(context, creator.Id);
        var result = await controller.InviteUser(room.Id, guest.Username);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Felhasználó meghívva a szobába.", ok.Value);

        var membership = await context.ChatRoomUsers
            .FirstOrDefaultAsync(cu => cu.ChatRoomId == room.Id && cu.UserId == guest.Id);
        Assert.NotNull(membership);
    }

    [Fact]
    public async Task SendMessage_InRoom_PersistsMessage()
    {
        var context = TestHelper.CreateContext(nameof(SendMessage_InRoom_PersistsMessage));
        var user = new User { Username = "sender", Email = "sender@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var room = new ChatRoom();
        room.Users.Add(new ChatRoomUser { User = user });

        context.Users.Add(user);
        context.ChatRooms.Add(room);
        await context.SaveChangesAsync();

        var controller = BuildController(context, user.Id);
        var result = await controller.SendMessage(room.Id, null, "Hello world!");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Üzenet elküldve.", ok.Value);

        var message = await context.CommunityMessages.SingleAsync();
        Assert.Equal(room.Id, message.ChannelId);
        Assert.Equal(user.Id, message.SenderId);
        Assert.Equal("Hello world!", message.Content);
    }

    [Fact]
    public async Task GetMessages_ReturnsOrderedMessages()
    {
        var context = TestHelper.CreateContext(nameof(GetMessages_ReturnsOrderedMessages));
        var user = new User { Username = "sender", Email = "sender@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.CommunityMessages.AddRange(
            new CommunityMessage { SenderId = user.Id, ChannelId = 1, Content = "first", Timestamp = DateTime.UtcNow.AddMinutes(-1) },
            new CommunityMessage { SenderId = user.Id, ChannelId = 1, Content = "second", Timestamp = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var controller = BuildController(context, user.Id);
        var result = await controller.GetMessages(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var messages = Assert.IsType<System.Collections.Generic.List<MessageDto>>(ok.Value);
        Assert.Equal(2, messages.Count);
        Assert.Equal("first", messages[0].Content);
        Assert.Equal("second", messages[1].Content);
    }

    [Fact]
    public async Task LeaveRoom_RemovesMembership()
    {
        var context = TestHelper.CreateContext(nameof(LeaveRoom_RemovesMembership));
        var user = new User { Username = "member", Email = "member@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var room = new ChatRoom();
        var membership = new ChatRoomUser { ChatRoom = room, User = user };

        room.Users.Add(membership);
        context.ChatRooms.Add(room);
        context.ChatRoomUsers.Add(membership);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = BuildController(context, user.Id);
        var result = await controller.LeaveRoom(room.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Kiléptél a szobából.", ok.Value);
        Assert.False(await context.ChatRoomUsers.AnyAsync(cu => cu.ChatRoomId == room.Id && cu.UserId == user.Id));
    }
}
