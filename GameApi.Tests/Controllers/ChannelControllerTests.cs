using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameApi.Controllers;
using GameApi.Data;
using GameApi.DTOs;
using GameApi.Models;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Tests.Controllers;

public class ChannelControllerTests
{
    [Fact]
    public async Task GetChannels_ReturnsOnlyCommunityChannels()
    {
        var context = TestHelper.CreateContext(nameof(GetChannels_ReturnsOnlyCommunityChannels));
        context.Channels.AddRange(
            new Channel { CommunityId = 10, Name = "General", Type = ChannelType.Text, IsPrivate = false },
            new Channel { CommunityId = 11, Name = "Other", Type = ChannelType.Voice, IsPrivate = true }
        );
        await context.SaveChangesAsync();

        var controller = new ChannelController(context);
        var result = await controller.GetChannels(10);

        var channels = Assert.IsType<List<ChannelReadDto>>(result.Value);
        Assert.Single(channels);
        Assert.All(channels, c => Assert.Equal(10, c.CommunityId));
    }

    [Fact]
    public async Task CreateChannel_ReturnsCreatedAndPersists()
    {
        var context = TestHelper.CreateContext(nameof(CreateChannel_ReturnsCreatedAndPersists));
        var controller = new ChannelController(context);
        var dto = new ChannelCreateDto
        {
            CommunityId = 7,
            Name = "strategy",
            Type = ChannelType.Text,
            IsPrivate = true
        };

        var result = await controller.CreateChannel(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ChannelController.GetChannels), created.ActionName);

        var persisted = await context.Channels.SingleAsync();
        Assert.Equal(dto.Name, persisted.Name);
        Assert.Equal(dto.CommunityId, persisted.CommunityId);
        Assert.Equal(dto.IsPrivate, persisted.IsPrivate);
    }
}
