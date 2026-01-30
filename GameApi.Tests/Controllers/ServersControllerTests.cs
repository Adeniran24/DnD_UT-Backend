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

public class ServersControllerTests
{
    private static ServersController CreateController(AppDbContext context, int userId)
    {
        var controller = new ServersController(context);
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
    public async Task GetServers_ContainsPublicAndMembershipServers()
    {
        var context = TestHelper.CreateContext(nameof(GetServers_ContainsPublicAndMembershipServers));
        var user = new User { Id = 1, Username = "owner", Email = "owner@example.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var publicServer = new Community { Name = "Public", Description = "desc", OwnerId = 2, IsPrivate = false };
        var privateServer = new Community { Name = "Private", Description = "desc", OwnerId = 3, IsPrivate = true };
        var hiddenServer = new Community { Name = "Hidden", Description = "desc", OwnerId = 4, IsPrivate = true };

        context.Users.Add(user);
        context.Communities.AddRange(publicServer, privateServer, hiddenServer);
        context.CommunityUsers.Add(new CommunityUser { Community = privateServer, User = user, Role = CommunityRole.Member });
        await context.SaveChangesAsync();

        var controller = CreateController(context, user.Id);
        var result = await controller.GetServers();

        var list = Assert.IsAssignableFrom<List<ServerSummaryDto>>(result.Value);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, s => s.Name == "Public");
        Assert.Contains(list, s => s.Name == "Private");
    }

    [Fact]
    public async Task GetServer_ForbidsAccessToPrivateServerWhenNotMember()
    {
        var context = TestHelper.CreateContext(nameof(GetServer_ForbidsAccessToPrivateServerWhenNotMember));
        var user = new User { Id = 9, Username = "guest", Email = "guest@example.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var privateServer = new Community { Name = "Secret", Description = "desc", OwnerId = 8, IsPrivate = true };

        context.Users.Add(user);
        context.Communities.Add(privateServer);
        await context.SaveChangesAsync();

        var controller = CreateController(context, user.Id);
        var result = await controller.GetServer(privateServer.Id);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CreateServer_PersistsServerWithOwnerAndChannels()
    {
        var context = TestHelper.CreateContext(nameof(CreateServer_PersistsServerWithOwnerAndChannels));
        var userId = 33;
        var user = new User { Id = userId, Username = "owner", Email = "owner@example.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId);
        var dto = new ServerCreateDto
        {
            Name = "  Test Server  ",
            Description = "  desc  ",
            IsPrivate = true,
            CoverImage = "/uploads/cover.png"
        };

        var actionResult = await controller.CreateServer(dto);
        var created = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var summary = Assert.IsType<ServerSummaryDto>(created.Value);

        Assert.Equal("Test Server", summary.Name);
        Assert.Equal("desc", summary.Description);
        Assert.Equal(userId, summary.OwnerId);

        var membership = await context.CommunityUsers.FirstOrDefaultAsync(cu => cu.UserId == userId && cu.CommunityId == summary.Id);
        Assert.NotNull(membership);
        Assert.Equal(CommunityRole.Owner, membership!.Role);

        var channelCount = await context.Channels.CountAsync(ch => ch.CommunityId == summary.Id);
        Assert.Equal(5, channelCount);
    }

    [Fact]
    public async Task UpdateServer_ForbidsMemberRole()
    {
        var context = TestHelper.CreateContext(nameof(UpdateServer_ForbidsMemberRole));
        var userId = 21;
        var community = new Community { Name = "Guild", Description = "desc", OwnerId = 5, IsPrivate = false };
        context.Communities.Add(community);
        context.CommunityUsers.Add(new CommunityUser { Community = community, UserId = userId, Role = CommunityRole.Member });
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId);
        var result = await controller.UpdateServer(community.Id, new ServerUpdateDto { Description = "change" });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteServer_AllowsOwnerAndRemovesCommunity()
    {
        var context = TestHelper.CreateContext(nameof(DeleteServer_AllowsOwnerAndRemovesCommunity));
        var ownerId = 66;
        var community = new Community { Name = "Drop", Description = "desc", OwnerId = ownerId, IsPrivate = false };
        context.Communities.Add(community);
        context.CommunityUsers.Add(new CommunityUser { Community = community, UserId = ownerId, Role = CommunityRole.Owner });
        await context.SaveChangesAsync();

        var controller = CreateController(context, ownerId);
        var result = await controller.DeleteServer(community.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await context.Communities.AnyAsync(c => c.Id == community.Id));
    }

    [Fact]
    public async Task DeleteServer_ForbidsNonOwner()
    {
        var context = TestHelper.CreateContext(nameof(DeleteServer_ForbidsNonOwner));
        var ownerId = 50;
        var otherId = 51;
        var community = new Community { Name = "Keep", Description = "desc", OwnerId = ownerId, IsPrivate = false };
        context.Communities.Add(community);
        context.CommunityUsers.Add(new CommunityUser { Community = community, UserId = otherId, Role = CommunityRole.Admin });
        await context.SaveChangesAsync();

        var controller = CreateController(context, otherId);
        var result = await controller.DeleteServer(community.Id);

        Assert.IsType<ForbidResult>(result);
    }
}
