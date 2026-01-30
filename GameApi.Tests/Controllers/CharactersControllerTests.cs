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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public async Task Update_ReturnsNotFound_WhenCharacterDoesNotBelongToCaller()
    {
        var context = TestHelper.CreateContext(nameof(Update_ReturnsNotFound_WhenCharacterDoesNotBelongToCaller));
        context.Characters.Add(new Character { userId = 99, characterName = "other" });
        await context.SaveChangesAsync();

        var controller = CreateController(context, 5);
        var result = await controller.Update(1, new Character { characterName = "attempt" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
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

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenCharacterMissing()
    {
        var context = TestHelper.CreateContext(nameof(Delete_ReturnsNotFound_WhenCharacterMissing));
        var controller = CreateController(context, 6);

        var result = await controller.Delete(900);
        Assert.IsType<NotFoundResult>(result);
    }
}
