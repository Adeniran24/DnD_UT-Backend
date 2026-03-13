using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameApi.Controllers;
using GameApi.Data;
using GameApi.Models;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Tests.Controllers;

public class CharactersControllerTests
{
    private static CharactersController CreateController(AppDbContext context, int userId)
    {
        var controller = new CharactersController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = TestHelper.CreateUserPrincipal(userId)
            }
        };

        return controller;
    }

    [Fact(DisplayName = "Get All Returns Only Characters Owned By Caller.")]
    public async Task GetAll_ReturnsOnlyCharactersOwnedByCaller()
    {
        var context = TestHelper.CreateContext(nameof(GetAll_ReturnsOnlyCharactersOwnedByCaller));
        context.Characters.Add(new Character { userId = 1, characterName = "alpha" });
        context.Characters.Add(new Character { userId = 2, characterName = "beta" });
        await context.SaveChangesAsync();

        var controller = CreateController(context, 1);
        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var characters = Assert.IsType<List<Character>>(ok.Value);
        Assert.Single(characters);
        Assert.Equal("alpha", characters[0].characterName);
    }

    [Fact(DisplayName = "Get By Id returns character for owner.")]
    public async Task GetById_ReturnsCharacter_ForOwner()
    {
        var context = TestHelper.CreateContext(nameof(GetById_ReturnsCharacter_ForOwner));
        var owned = new Character { userId = 7, characterName = "owner-char" };
        context.Characters.Add(owned);
        await context.SaveChangesAsync();

        var controller = CreateController(context, 7);
        var result = await controller.GetById(owned.id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var character = Assert.IsType<Character>(ok.Value);
        Assert.Equal("owner-char", character.characterName);
    }

    [Fact(DisplayName = "Get By Id returns not found for foreign character.")]
    public async Task GetById_ReturnsNotFound_ForForeignCharacter()
    {
        var context = TestHelper.CreateContext(nameof(GetById_ReturnsNotFound_ForForeignCharacter));
        var foreign = new Character { userId = 99, characterName = "foreign" };
        context.Characters.Add(foreign);
        await context.SaveChangesAsync();

        var controller = CreateController(context, 1);
        var result = await controller.GetById(foreign.id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact(DisplayName = "Create Normalizes Json And Assigns Ownership.")]
    public async Task Create_NormalizesJsonAndAssignsOwnership()
    {
        var context = TestHelper.CreateContext(nameof(Create_NormalizesJsonAndAssignsOwnership));
        var controller = CreateController(context, 7);

        var payload = new Character
        {
            characterName = "Nova",
            equipment = "not json",
            attacks = "also bad",
            spellbook = "nope",
            featuresFeats = ""
        };

        var result = await controller.Create(payload);
        var ok = Assert.IsType<OkObjectResult>(result);
        var created = Assert.IsType<Character>(ok.Value);

        Assert.Equal("{}", created.equipment);
        Assert.Equal("[]", created.attacks);
        Assert.Equal("[]", created.spellbook);
        Assert.Equal("[]", created.featuresFeats);
        Assert.Equal(7, created.userId);
        Assert.NotEqual(0, created.id);
        Assert.InRange(created.updated_at, created.created_at, DateTime.UtcNow.AddSeconds(1));
    }

    [Fact(DisplayName = "Create returns bad request for null payload.")]
    public async Task Create_ReturnsBadRequest_ForNullPayload()
    {
        var context = TestHelper.CreateContext(nameof(Create_ReturnsBadRequest_ForNullPayload));
        var controller = CreateController(context, 7);

        var result = await controller.Create(null!);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Character payload is required.", badRequest.Value);
    }

    [Fact(DisplayName = "Update Changes Fields And Keeps Ownership.")]
    public async Task Update_ChangesFieldsAndKeepsOwnership()
    {
        var context = TestHelper.CreateContext(nameof(Update_ChangesFieldsAndKeepsOwnership));
        var existing = new Character
        {
            userId = 5,
            characterName = "OldName",
            equipment = "{}",
            attacks = "[]",
            spellbook = "[]",
            featuresFeats = "[]",
            created_at = DateTime.UtcNow.AddDays(-1),
            updated_at = DateTime.UtcNow.AddDays(-1)
        };
        context.Characters.Add(existing);
        await context.SaveChangesAsync();

        var controller = CreateController(context, 5);
        var payload = new Character
        {
            characterName = "NewName",
            equipment = "invalid",
            attacks = "invalid",
            spellbook = "invalid",
            featuresFeats = "invalid"
        };

        var originalUpdatedAt = existing.updated_at;
        var result = await controller.Update(existing.id, payload);
        var ok = Assert.IsType<OkObjectResult>(result);
        var persisted = Assert.IsType<Character>(ok.Value);

        Assert.Equal("NewName", persisted.characterName);
        Assert.Equal("{}", persisted.equipment);
        Assert.Equal("[]", persisted.attacks);
        Assert.Equal("[]", persisted.spellbook);
        Assert.Equal("[]", persisted.featuresFeats);
        Assert.Equal(existing.created_at, persisted.created_at);
        Assert.True(persisted.updated_at > originalUpdatedAt);
    }

    [Fact(DisplayName = "Update returns bad request for null payload.")]
    public async Task Update_ReturnsBadRequest_ForNullPayload()
    {
        var context = TestHelper.CreateContext(nameof(Update_ReturnsBadRequest_ForNullPayload));
        var existing = new Character { userId = 1, characterName = "x" };
        context.Characters.Add(existing);
        await context.SaveChangesAsync();

        var controller = CreateController(context, 1);
        var result = await controller.Update(existing.id, null!);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Character payload is required.", badRequest.Value);
    }

    [Fact(DisplayName = "Update Returns Not Found When Character Does Not Belong To Caller.")]
    public async Task Update_ReturnsNotFound_WhenCharacterDoesNotBelongToCaller()
    {
        var context = TestHelper.CreateContext(nameof(Update_ReturnsNotFound_WhenCharacterDoesNotBelongToCaller));
        context.Characters.Add(new Character { userId = 99, characterName = "other" });
        await context.SaveChangesAsync();

        var controller = CreateController(context, 5);
        var result = await controller.Update(1, new Character { characterName = "attempt" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact(DisplayName = "Delete Removes Owned Character.")]
    public async Task Delete_RemovesOwnedCharacter()
    {
        var context = TestHelper.CreateContext(nameof(Delete_RemovesOwnedCharacter));
        var character = new Character { userId = 6, characterName = "victim" };
        context.Characters.Add(character);
        await context.SaveChangesAsync();

        var controller = CreateController(context, 6);
        var result = await controller.Delete(character.id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await context.Characters.AnyAsync());
    }

    [Fact(DisplayName = "Delete Returns Not Found When Character Missing.")]
    public async Task Delete_ReturnsNotFound_WhenCharacterMissing()
    {
        var context = TestHelper.CreateContext(nameof(Delete_ReturnsNotFound_WhenCharacterMissing));
        var controller = CreateController(context, 6);

        var result = await controller.Delete(900);
        Assert.IsType<NotFoundResult>(result);
    }
}
