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

    [Fact(DisplayName = "Create Room Creates Room And Adds Creator.")]
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

    [Fact(DisplayName = "Invite User Forbids Non Member.")]
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

    [Fact(DisplayName = "Invite User returns not found when room is missing.")]
    public async Task InviteUser_ReturnsNotFound_WhenRoomMissing()
    {
        var context = TestHelper.CreateContext(nameof(InviteUser_ReturnsNotFound_WhenRoomMissing));
        context.Users.Add(new User { Id = 1, Username = "alice", Email = "alice@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.InviteUser(999, "alice");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Szoba nem található.", notFound.Value);
    }

    [Fact(DisplayName = "Invite User returns not found when target user is missing.")]
    public async Task InviteUser_ReturnsNotFound_WhenTargetUserMissing()
    {
        var context = TestHelper.CreateContext(nameof(InviteUser_ReturnsNotFound_WhenTargetUserMissing));
        var inviter = new User { Id = 1, Username = "inviter", Email = "inviter@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var room = new ChatRoom();
        room.Users.Add(new ChatRoomUser { UserId = inviter.Id });
        context.Users.Add(inviter);
        context.ChatRooms.Add(room);
        await context.SaveChangesAsync();

        var controller = BuildController(context, inviter.Id);
        var result = await controller.InviteUser(room.Id, "missing-user");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Felhasználó nem található.", notFound.Value);
    }

    [Fact(DisplayName = "Invite User rejects already joined member.")]
    public async Task InviteUser_RejectsAlreadyJoinedMember()
    {
        var context = TestHelper.CreateContext(nameof(InviteUser_RejectsAlreadyJoinedMember));
        var inviter = new User { Id = 1, Username = "inviter", Email = "inviter@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var member = new User { Id = 2, Username = "member", Email = "member@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var room = new ChatRoom();
        room.Users.Add(new ChatRoomUser { UserId = inviter.Id });
        room.Users.Add(new ChatRoomUser { UserId = member.Id });
        context.Users.AddRange(inviter, member);
        context.ChatRooms.Add(room);
        await context.SaveChangesAsync();

        var controller = BuildController(context, inviter.Id);
        var result = await controller.InviteUser(room.Id, member.Username);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Felhasználó már a szobában van.", badRequest.Value);
    }

    [Fact(DisplayName = "Invite User Adds User When Inviter Is Member.")]
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

    [Fact(DisplayName = "Send Message In Room Persists Message.")]
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

    [Fact(DisplayName = "Join Room returns not found when room missing.")]
    public async Task JoinRoom_ReturnsNotFound_WhenRoomMissing()
    {
        var context = TestHelper.CreateContext(nameof(JoinRoom_ReturnsNotFound_WhenRoomMissing));
        var controller = BuildController(context, 1);

        var result = await controller.JoinRoom(999);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Szoba nem található.", notFound.Value);
    }

    [Fact(DisplayName = "Join Room rejects when user already in room.")]
    public async Task JoinRoom_RejectsWhenAlreadyMember()
    {
        var context = TestHelper.CreateContext(nameof(JoinRoom_RejectsWhenAlreadyMember));
        var user = new User { Id = 10, Username = "already", Email = "already@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var room = new ChatRoom();
        room.Users.Add(new ChatRoomUser { UserId = user.Id });
        context.Users.Add(user);
        context.ChatRooms.Add(room);
        await context.SaveChangesAsync();

        var controller = BuildController(context, user.Id);
        var result = await controller.JoinRoom(room.Id);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Már csatlakoztál a szobához.", badRequest.Value);
    }

    [Fact(DisplayName = "Join Room adds membership when valid.")]
    public async Task JoinRoom_AddsMembership_WhenValid()
    {
        var context = TestHelper.CreateContext(nameof(JoinRoom_AddsMembership_WhenValid));
        var owner = new User { Id = 1, Username = "owner", Email = "owner@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var joiner = new User { Id = 2, Username = "joiner", Email = "joiner@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var room = new ChatRoom();
        room.Users.Add(new ChatRoomUser { UserId = owner.Id });
        context.Users.AddRange(owner, joiner);
        context.ChatRooms.Add(room);
        await context.SaveChangesAsync();

        var controller = BuildController(context, joiner.Id);
        var result = await controller.JoinRoom(room.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Csatlakozás sikeres.", ok.Value);
        Assert.True(await context.ChatRoomUsers.AnyAsync(u => u.ChatRoomId == room.Id && u.UserId == joiner.Id));
    }

    [Fact(DisplayName = "Send Message returns not found when room missing.")]
    public async Task SendMessage_ReturnsNotFound_WhenRoomMissing()
    {
        var context = TestHelper.CreateContext(nameof(SendMessage_ReturnsNotFound_WhenRoomMissing));
        var user = new User { Id = 1, Username = "sender", Email = "sender@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = BuildController(context, user.Id);
        var result = await controller.SendMessage(555, null, "hello");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Szoba nem található.", notFound.Value);
    }

    [Fact(DisplayName = "Send Message forbids non member in room.")]
    public async Task SendMessage_ForbidsNonMemberInRoom()
    {
        var context = TestHelper.CreateContext(nameof(SendMessage_ForbidsNonMemberInRoom));
        var owner = new User { Id = 1, Username = "owner", Email = "owner@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var outsider = new User { Id = 2, Username = "outsider", Email = "outsider@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var room = new ChatRoom();
        room.Users.Add(new ChatRoomUser { UserId = owner.Id });
        context.Users.AddRange(owner, outsider);
        context.ChatRooms.Add(room);
        await context.SaveChangesAsync();

        var controller = BuildController(context, outsider.Id);
        var result = await controller.SendMessage(room.Id, null, "hello");

        Assert.IsType<ForbidResult>(result);
    }

    [Fact(DisplayName = "Send Message returns not found for missing private recipient.")]
    public async Task SendMessage_ReturnsNotFound_ForMissingRecipient()
    {
        var context = TestHelper.CreateContext(nameof(SendMessage_ReturnsNotFound_ForMissingRecipient));
        var user = new User { Id = 1, Username = "sender", Email = "sender@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = BuildController(context, user.Id);
        var result = await controller.SendMessage(null, "ghost", "hello");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Felhasználó nem található.", notFound.Value);
    }

    [Fact(DisplayName = "Send Message rejects private message mode.")]
    public async Task SendMessage_RejectsPrivateMessageMode()
    {
        var context = TestHelper.CreateContext(nameof(SendMessage_RejectsPrivateMessageMode));
        var sender = new User { Id = 1, Username = "sender", Email = "sender@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        var receiver = new User { Id = 2, Username = "receiver", Email = "receiver@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(sender, receiver);
        await context.SaveChangesAsync();

        var controller = BuildController(context, sender.Id);
        var result = await controller.SendMessage(null, receiver.Username, "hello");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Privát üzenetek még nem támogatottak ebben a verzióban.", badRequest.Value);
    }

    [Fact(DisplayName = "Send Message requires room or recipient.")]
    public async Task SendMessage_RequiresRoomOrRecipient()
    {
        var context = TestHelper.CreateContext(nameof(SendMessage_RequiresRoomOrRecipient));
        var sender = new User { Id = 1, Username = "sender", Email = "sender@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        context.Users.Add(sender);
        await context.SaveChangesAsync();

        var controller = BuildController(context, sender.Id);
        var result = await controller.SendMessage(null, null, "hello");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Adj meg szobát vagy felhasználót.", badRequest.Value);
    }

    [Fact(DisplayName = "Get Messages Returns Ordered Messages.")]
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

    [Fact(DisplayName = "Leave Room Removes Membership.")]
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

    [Fact(DisplayName = "Leave Room returns not found when membership missing.")]
    public async Task LeaveRoom_ReturnsNotFound_WhenMembershipMissing()
    {
        var context = TestHelper.CreateContext(nameof(LeaveRoom_ReturnsNotFound_WhenMembershipMissing));
        var user = new User { Id = 1, Username = "member", Email = "member@example.com", PasswordHash = string.Empty, CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = BuildController(context, user.Id);
        var result = await controller.LeaveRoom(321);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Nem vagy tagja a szobának.", notFound.Value);
    }
}
