using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using GameApi.Controllers;
using GameApi.Data;
using GameApi.Models;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Tests.Controllers;

public class AuthControllerTests
{
    private static (AuthController Controller, AppDbContext Context) BuildController(string databaseName)
    {
        var config = TestHelper.CreateJwtConfiguration();
        var env = TestHelper.CreateWebHostEnvironment();
        var context = TestHelper.CreateContext(databaseName);
        var controller = new AuthController(context, config, env);
        return (controller, context);
    }

    private static string Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }

    [Fact(DisplayName = "Register Returns Bad Request When Missing Data.")]
    public async Task Register_ReturnsBadRequest_WhenMissingData()
    {
        var (controller, _) = BuildController(nameof(Register_ReturnsBadRequest_WhenMissingData));
        var result = await controller.Register(email: string.Empty, username: "user", password: "hash");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Missing data.", badRequest.Value);
    }

    [Fact(DisplayName = "Register Detects Duplicate Email.")]
    public async Task Register_DetectsDuplicateEmail()
    {
        var (controller, context) = BuildController(nameof(Register_DetectsDuplicateEmail));
        context.Users.Add(new User { Email = "dup@example.com", Username = "existing", PasswordHash = Hash("x"), CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var result = await controller.Register("dup@example.com", "new", "hash");
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Email already registered.", badRequest.Value);
    }

    [Fact(DisplayName = "Register Saves User With Hashed Password.")]
    public async Task Register_SavesUserWithHashedPassword()
    {
        var (controller, context) = BuildController(nameof(Register_SavesUserWithHashedPassword));
        var result = await controller.Register("new@example.com", "newuser", "hashvalue");

        Assert.IsType<OkResult>(result);

        var persisted = await context.Users.SingleAsync();
        Assert.Equal("newuser", persisted.Username);
        Assert.Equal("new@example.com", persisted.Email);
        Assert.Equal(Hash("hashvalue"), persisted.PasswordHash);
        Assert.True(persisted.IsActive);
    }

    [Fact(DisplayName = "Login Returns Bad Request When Missing Credentials.")]
    public async Task Login_ReturnsBadRequest_WhenMissingCredentials()
    {
        var (controller, _) = BuildController(nameof(Login_ReturnsBadRequest_WhenMissingCredentials));
        var result = await controller.Login(string.Empty, string.Empty);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Email and password are required.", badRequest.Value);
    }

    [Fact(DisplayName = "Login Returns Unauthorized When User Missing.")]
    public async Task Login_ReturnsUnauthorized_WhenUserMissing()
    {
        var (controller, _) = BuildController(nameof(Login_ReturnsUnauthorized_WhenUserMissing));
        var result = await controller.Login("missing@example.com", "hash");

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid credentials.", unauthorized.Value);
    }

    [Fact(DisplayName = "Login Returns Unauthorized When Wrong Password.")]
    public async Task Login_ReturnsUnauthorized_WhenWrongPassword()
    {
        var (controller, context) = BuildController(nameof(Login_ReturnsUnauthorized_WhenWrongPassword));
        context.Users.Add(new User
        {
            Email = "user@example.com",
            Username = "user",
            PasswordHash = Hash("correct"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var result = await controller.Login("user@example.com", "wrong");
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid credentials.", unauthorized.Value);
    }

    [Fact(DisplayName = "Login Returns Unauthorized When User Banned.")]
    public async Task Login_ReturnsUnauthorized_WhenUserBanned()
    {
        var (controller, context) = BuildController(nameof(Login_ReturnsUnauthorized_WhenUserBanned));
        context.Users.Add(new User
        {
            Email = "banned@example.com",
            Username = "banned",
            PasswordHash = Hash("secret"),
            CreatedAt = DateTime.UtcNow,
            IsActive = false
        });
        await context.SaveChangesAsync();

        var result = await controller.Login("banned@example.com", "secret");
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User is banned.", unauthorized.Value);
    }

    [Fact(DisplayName = "Login Returns Token For Valid User.")]
    public async Task Login_ReturnsToken_ForValidUser()
    {
        var (controller, context) = BuildController(nameof(Login_ReturnsToken_ForValidUser));
        var user = new User
        {
            Email = "ok@example.com",
            Username = "ok",
            PasswordHash = Hash("pw"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Role = "User"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await controller.Login("ok@example.com", "pw");
        var ok = Assert.IsType<OkObjectResult>(result);

        var tokenProperty = ok.Value?.GetType().GetProperty("token");
        Assert.NotNull(tokenProperty);
        var token = Assert.IsType<string>(tokenProperty!.GetValue(ok.Value)!);
        Assert.False(string.IsNullOrWhiteSpace(token));

        var reloaded = await context.Users.SingleAsync(u => u.Email == "ok@example.com");
        Assert.NotNull(reloaded.LastLoginAt);
    }
}
