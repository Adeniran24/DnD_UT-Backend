using System;
using System.Collections.Generic;
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

public class InvitesControllerTests
{
    private static InvitesController CreateController(AppDbContext context, int userId)
    {
        var controller = new InvitesController(context);
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
    public async Task CreateInvite_ForbidsMemberRole()
    {
        var context = TestHelper.CreateContext(nameof(CreateInvite_ForbidsMemberRole));
        var community = new Community { Name = "Club", Description = "desc", OwnerId = 1 };
        context.Communities.Add(community);
        context.CommunityUsers.Add(new CommunityUser { Community = community, UserId = 7, Role = CommunityRole.Member });
        await context.SaveChangesAsync();

        var controller = CreateController(context, 7);
        var result = await controller.CreateInvite(community.Id, new InviteCreateDto());

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CreateInvite_AllowsAdminAndReturnsCode()
    {
        var context = TestHelper.CreateContext(nameof(CreateInvite_AllowsAdminAndReturnsCode));
        var community = new Community { Name = "Guild", Description = "desc", OwnerId = 2 };
        context.Communities.Add(community);
        context.CommunityUsers.Add(new CommunityUser { Community = community, UserId = 5, Role = CommunityRole.Admin });
        await context.SaveChangesAsync();

        var controller = CreateController(context, 5);
        var dto = new InviteCreateDto { MaxUses = 3 };
        var result = await controller.CreateInvite(community.Id, dto);

        var inviteDto = Assert.IsType<InviteDto>(result.Value);
        Assert.Equal(community.Id, inviteDto.CommunityId);
        Assert.NotNull(inviteDto.Code);
        Assert.Equal(8, inviteDto.Code.Length);
        Assert.Equal(0, inviteDto.Uses);
        Assert.Equal(3, inviteDto.MaxUses);
    }

    [Fact]
    public async Task GetInvites_ReturnsSortedList()
    {
        var context = TestHelper.CreateContext(nameof(GetInvites_ReturnsSortedList));
        var community = new Community { Name = "Realm", Description = "desc", OwnerId = 3 };
        context.Communities.Add(community);
        context.CommunityUsers.Add(new CommunityUser { Community = community, UserId = 9, Role = CommunityRole.Admin });
        context.CommunityInvites.AddRange(
            new CommunityInvite { Community = community, Code = "a1", CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
            new CommunityInvite { Community = community, Code = "b2", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var controller = CreateController(context, 9);
        var result = await controller.GetInvites(community.Id);

        var list = Assert.IsAssignableFrom<List<InviteDto>>(result.Value);
        Assert.Equal(2, list.Count);
        Assert.Equal("b2", list[0].Code);
    }

    [Fact]
    public async Task JoinInvite_ReturnsNotFound_WhenMissing()
    {
        var context = TestHelper.CreateContext(nameof(JoinInvite_ReturnsNotFound_WhenMissing));
        var controller = CreateController(context, 9);

        var result = await controller.JoinInvite("missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task JoinInvite_ReturnsBadRequest_WhenExpired()
    {
        var context = TestHelper.CreateContext(nameof(JoinInvite_ReturnsBadRequest_WhenExpired));
        var invite = new CommunityInvite { Code = "abc", ExpiresAt = DateTime.UtcNow.AddMinutes(-5) };
        context.CommunityInvites.Add(invite);
        await context.SaveChangesAsync();

        var controller = CreateController(context, 10);
        var result = await controller.JoinInvite("abc");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invite expired.", badRequest.Value);
    }

    [Fact]
    public async Task JoinInvite_ReturnsBadRequest_WhenMaxUsesExceeded()
    {
        var context = TestHelper.CreateContext(nameof(JoinInvite_ReturnsBadRequest_WhenMaxUsesExceeded));
        var invite = new CommunityInvite { Code = "xyz", MaxUses = 1, Uses = 1 };
        context.CommunityInvites.Add(invite);
        await context.SaveChangesAsync();

        var controller = CreateController(context, 20);
        var result = await controller.JoinInvite("xyz");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invite has reached max uses.", badRequest.Value);
    }

    [Fact]
    public async Task JoinInvite_ReturnsNoContent_WhenAlreadyMember()
    {
        var context = TestHelper.CreateContext(nameof(JoinInvite_ReturnsNoContent_WhenAlreadyMember));
        var community = new Community { Name = "Club", Description = "desc", OwnerId = 5 };
        context.Communities.Add(community);
        context.CommunityUsers.Add(new CommunityUser { Community = community, UserId = 33, Role = CommunityRole.Member });
        var invite = new CommunityInvite { Community = community, Code = "mem", MaxUses = 2 };
        context.CommunityInvites.Add(invite);
        await context.SaveChangesAsync();

        var controller = CreateController(context, 33);
        var result = await controller.JoinInvite("mem");

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, invite.Uses);
    }

    [Fact]
    public async Task JoinInvite_AddsMemberAndIncrementsUses()
    {
        var context = TestHelper.CreateContext(nameof(JoinInvite_AddsMemberAndIncrementsUses));
        var community = new Community { Name = "Guild", Description = "desc", OwnerId = 2 };
        context.Communities.Add(community);
        var invite = new CommunityInvite { Community = community, Code = "joinme", MaxUses = 2 };
        context.CommunityInvites.Add(invite);
        await context.SaveChangesAsync();

        var controller = CreateController(context, 77);
        var result = await controller.JoinInvite("joinme");

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(1, await context.CommunityUsers.CountAsync(cu => cu.UserId == 77 && cu.CommunityId == community.Id));
        Assert.Equal(1, invite.Uses);
    }
}
