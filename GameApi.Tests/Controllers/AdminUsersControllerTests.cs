using System;
using System.Linq;
using GameApi.Controllers.Admin;
using GameApi.Data;
using GameApi.DTOs;
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

    [Fact(DisplayName = "Get Users Returns All Users.")]
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

    [Fact(DisplayName = "Update Role Rejects Changing Own Admin Role.")]
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

    [Fact(DisplayName = "Get User Returns Not Found When Missing.")]
    public async Task GetUser_ReturnsNotFound_WhenMissing()
    {
        var context = TestHelper.CreateContext(nameof(GetUser_ReturnsNotFound_WhenMissing));
        var controller = BuildController(context, 1);

        var result = await controller.GetUser(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact(DisplayName = "Get User Returns User When Exists.")]
    public async Task GetUser_ReturnsUser_WhenExists()
    {
        var context = TestHelper.CreateContext(nameof(GetUser_ReturnsUser_WhenExists));
        var user = new User
        {
            Id = 21,
            Email = "u@dnd.com",
            Username = "unit",
            PasswordHash = "x",
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.GetUser(user.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminUserDto>(ok.Value);
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal(user.Username, dto.Username);
    }

    [Fact(DisplayName = "Update Role Returns Bad Request For Invalid Role.")]
    public async Task UpdateRole_ReturnsBadRequest_ForInvalidRole()
    {
        var context = TestHelper.CreateContext(nameof(UpdateRole_ReturnsBadRequest_ForInvalidRole));
        context.Users.Add(new User { Id = 1, Email = "u@dnd.com", Username = "u", PasswordHash = "x", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = BuildController(context, adminId: 99);
        var result = await controller.UpdateRole(1, new UpdateUserRoleDto { Role = "SuperAdmin" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid role", badRequest.Value);
    }

    [Fact(DisplayName = "Update Role Returns Not Found When User Missing.")]
    public async Task UpdateRole_ReturnsNotFound_WhenUserMissing()
    {
        var context = TestHelper.CreateContext(nameof(UpdateRole_ReturnsNotFound_WhenUserMissing));
        var controller = BuildController(context, adminId: 50);

        var result = await controller.UpdateRole(404, new UpdateUserRoleDto { Role = "DM" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact(DisplayName = "Update Role Persists New Role When Valid.")]
    public async Task UpdateRole_PersistsNewRole_WhenValid()
    {
        var context = TestHelper.CreateContext(nameof(UpdateRole_PersistsNewRole_WhenValid));
        var target = new User { Id = 7, Email = "target@dnd.com", Username = "target", PasswordHash = "x", Role = "User", CreatedAt = DateTime.UtcNow };
        context.Users.Add(target);
        await context.SaveChangesAsync();

        var controller = BuildController(context, adminId: 99);
        var result = await controller.UpdateRole(target.Id, new UpdateUserRoleDto { Role = "DM" });

        Assert.IsType<NoContentResult>(result);
        var reloaded = await context.Users.FindAsync(target.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("DM", reloaded!.Role);
    }

    [Fact(DisplayName = "Update Status Rejects Deactivating Own Account.")]
    public async Task UpdateStatus_RejectsDeactivatingOwnAccount()
    {
        var context = TestHelper.CreateContext(nameof(UpdateStatus_RejectsDeactivatingOwnAccount));
        var admin = new User { Id = 10, Email = "admin@dnd.com", Username = "admin", PasswordHash = "x", Role = "Admin", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var controller = BuildController(context, admin.Id);
        var result = await controller.UpdateStatus(admin.Id, new UpdateUserStatusDto { IsActive = false });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("You cannot deactivate your own account.", badRequest.Value);
    }

    [Fact(DisplayName = "Update Status Returns Not Found When User Missing.")]
    public async Task UpdateStatus_ReturnsNotFound_WhenUserMissing()
    {
        var context = TestHelper.CreateContext(nameof(UpdateStatus_ReturnsNotFound_WhenUserMissing));
        var controller = BuildController(context, adminId: 1);

        var result = await controller.UpdateStatus(222, new UpdateUserStatusDto { IsActive = true });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact(DisplayName = "Update Status Persists Activation Change.")]
    public async Task UpdateStatus_PersistsActivationChange()
    {
        var context = TestHelper.CreateContext(nameof(UpdateStatus_PersistsActivationChange));
        var target = new User
        {
            Id = 35,
            Email = "inactive@dnd.com",
            Username = "inactive",
            PasswordHash = "x",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(target);
        await context.SaveChangesAsync();

        var controller = BuildController(context, adminId: 99);
        var result = await controller.UpdateStatus(target.Id, new UpdateUserStatusDto { IsActive = true });

        Assert.IsType<NoContentResult>(result);
        var reloaded = await context.Users.FindAsync(target.Id);
        Assert.NotNull(reloaded);
        Assert.True(reloaded!.IsActive);
    }
}
