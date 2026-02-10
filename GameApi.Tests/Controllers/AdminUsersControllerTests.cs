using System;
using System.Linq;
using GameApi.Controllers.Admin;
using GameApi.Data;
using GameApi.Models;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameApi.Tests.Controllers;

public class AdminUsersControllerTests
{
    private static AdminUsersController BuildController(AppDbContext context, int adminId)
    {
        var controller = new AdminUsersController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = TestHelper.CreateUserPrincipal(adminId)
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        var context = TestHelper.CreateContext(nameof(GetUsers_ReturnsAllUsers));
        context.Users.AddRange(
            new User { Email = "a@a.com", Username = "a", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Email = "b@b.com", Username = "b", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.GetUsers();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<object>>(ok.Value);
        Assert.Equal(2, users.Count());
    }

    [Fact]
    public async Task UpdateRole_RejectsChangingOwnAdminRole()
    {
        var context = TestHelper.CreateContext(nameof(UpdateRole_RejectsChangingOwnAdminRole));
        var admin = new User { Id = 10, Email = "admin@dnd.com", Username = "admin", PasswordHash = "x", Role = "Admin", CreatedAt = DateTime.UtcNow };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var controller = BuildController(context, admin.Id);
        var result = await controller.UpdateRole(admin.Id, new GameApi.DTOs.UpdateUserRoleDto { Role = "User" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("You cannot change your own admin role.", badRequest.Value);
    }
}
