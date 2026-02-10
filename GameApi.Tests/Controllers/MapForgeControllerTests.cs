using System;
using GameApi.Controllers;
using GameApi.Data;
using GameApi.Models;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameApi.Tests.Controllers;

public class MapForgeControllerTests
{
    private static MapForgeController BuildController(AppDbContext context, int userId)
    {
        var env = TestHelper.CreateWebHostEnvironment();
        var controller = new MapForgeController(context, env);
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
    public async Task CreateCampaign_PersistsCampaign()
    {
        var context = TestHelper.CreateContext(nameof(CreateCampaign_PersistsCampaign));
        context.Users.Add(new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.CreateCampaign(new MapForgeController.CreateCampaignRequest
        {
            Name = "My Campaign",
            SeedStarter = false
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Single(context.MapCampaigns);
    }
}
