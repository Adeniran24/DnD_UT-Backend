using GameApi.Data;
using GameApi.Models;
using GameApi.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Tests.Data;

public class AppDbContextModelTests
{
    [Fact(DisplayName = "ChatRoomUser has composite primary key.")]
    public void ChatRoomUser_HasCompositePrimaryKey()
    {
        using var context = TestHelper.CreateContext(nameof(ChatRoomUser_HasCompositePrimaryKey));
        var entityType = context.Model.FindEntityType(typeof(ChatRoomUser));

        Assert.NotNull(entityType);
        var key = entityType!.FindPrimaryKey();
        Assert.NotNull(key);
        Assert.Equal(new[] { "ChatRoomId", "UserId" }, key!.Properties.Select(p => p.Name));
    }

    [Fact(DisplayName = "Friendship uses restrict delete behavior for both sides.")]
    public void Friendship_UsesRestrictDeleteBehavior()
    {
        using var context = TestHelper.CreateContext(nameof(Friendship_UsesRestrictDeleteBehavior));
        var entityType = context.Model.FindEntityType(typeof(Friendship));

        Assert.NotNull(entityType);
        var foreignKeys = entityType!.GetForeignKeys().ToList();
        Assert.Equal(2, foreignKeys.Count);
        Assert.All(foreignKeys, fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }

    [Fact(DisplayName = "CommunityInvite has unique index on code.")]
    public void CommunityInvite_HasUniqueCodeIndex()
    {
        using var context = TestHelper.CreateContext(nameof(CommunityInvite_HasUniqueCodeIndex));
        var entityType = context.Model.FindEntityType(typeof(CommunityInvite));

        Assert.NotNull(entityType);
        var codeIndex = entityType!.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == "Code");

        Assert.NotNull(codeIndex);
        Assert.True(codeIndex!.IsUnique);
    }

    [Fact(DisplayName = "Character created_at and updated_at default SQL are configured.")]
    public void Character_Timestamps_HaveDefaultSql()
    {
        using var context = TestHelper.CreateContext(nameof(Character_Timestamps_HaveDefaultSql));
        var entityType = context.Model.FindEntityType(typeof(Character));

        Assert.NotNull(entityType);
        var createdAt = entityType!.FindProperty("created_at");
        var updatedAt = entityType.FindProperty("updated_at");

        Assert.NotNull(createdAt);
        Assert.NotNull(updatedAt);
        Assert.Equal("CURRENT_TIMESTAMP", createdAt!.GetDefaultValueSql());
        Assert.Equal("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP", updatedAt!.GetDefaultValueSql());
    }

    [Fact(DisplayName = "VttSessionMember has composite primary key.")]
    public void VttSessionMember_HasCompositePrimaryKey()
    {
        using var context = TestHelper.CreateContext(nameof(VttSessionMember_HasCompositePrimaryKey));
        var entityType = context.Model.FindEntityType(typeof(VttSessionMember));

        Assert.NotNull(entityType);
        var key = entityType!.FindPrimaryKey();
        Assert.NotNull(key);
        Assert.Equal(new[] { "SessionId", "UserId" }, key!.Properties.Select(p => p.Name));
    }
}
