using System;
using System.Linq;
using GameApi.Controllers;
using GameApi.Data;
using GameApi.Hubs;
using GameApi.Models;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameApi.Tests.Controllers;

public class VttControllerTests
{
    private static VttController BuildController(AppDbContext context, int userId)
    {
        var env = TestHelper.CreateWebHostEnvironment();
        var hub = new FakeHubContext<VttHub>();
        var controller = new VttController(context, env, hub);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = TestHelper.CreateUserPrincipal(userId)
            }
        };
        return controller;
    }

    [Fact(DisplayName = "Create Session Creates Session And Map.")]
    public async Task CreateSession_CreatesSessionAndMap()
    {
        var context = TestHelper.CreateContext(nameof(CreateSession_CreatesSessionAndMap));
        var user = new User { Id = 1, Username = "dm", Email = "dm@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = BuildController(context, user.Id);
        var result = await controller.CreateSession(new VttController.CreateSessionRequest { Name = "Test Session" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Single(context.VttSessions);
        Assert.Single(context.VttMaps);
        Assert.Single(context.VttSessionMembers);
    }

    [Fact(DisplayName = "Get Sessions Returns Owned Sessions.")]
    public async Task GetSessions_ReturnsOwnedSessions()
    {
        var context = TestHelper.CreateContext(nameof(GetSessions_ReturnsOwnedSessions));
        var user = new User { Id = 2, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        context.VttSessions.Add(new VttSession
        {
            Name = "Owned",
            OwnerUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, user.Id);
        var result = await controller.GetSessions();

        var ok = Assert.IsType<OkObjectResult>(result);
        var sessions = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<object>>(ok.Value);
        Assert.Single(sessions);
    }
}
