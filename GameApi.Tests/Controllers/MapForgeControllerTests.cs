using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GameApi.Controllers;
using GameApi.Data;
using GameApi.Models;
using GameApi.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Tests.Controllers;

public class MapForgeControllerTests
{
    private static readonly JsonSerializerOptions ReadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

    private static MapForgeController.MapCampaign BuildCampaignEntity(string id, int ownerUserId, string name = "Campaign")
    {
        var nodes = new List<MapForgeController.FlowNode>
        {
            new()
            {
                Id = "n1",
                Position = new MapForgeController.Position { X = 0, Y = 0 },
                Data = new MapForgeController.NodeData { Label = "One", Type = "Location" }
            },
            new()
            {
                Id = "n2",
                Position = new MapForgeController.Position { X = 20, Y = 20 },
                Data = new MapForgeController.NodeData { Label = "Two", Type = "Location" }
            }
        };

        var edges = new List<MapForgeController.FlowEdge>
        {
            new() { Id = "n1-n2", Source = "n1", Target = "n2", Type = "smoothstep" }
        };

        return new MapForgeController.MapCampaign
        {
            Id = id,
            OwnerUserId = ownerUserId,
            Name = name,
            NodesJson = JsonSerializer.Serialize(nodes),
            EdgesJson = JsonSerializer.Serialize(edges),
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    [Fact(DisplayName = "Create Campaign Persists Campaign.")]
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

    [Fact(DisplayName = "Get Campaign returns not found when campaign is missing.")]
    public async Task GetCampaign_ReturnsNotFound_WhenMissing()
    {
        var context = TestHelper.CreateContext(nameof(GetCampaign_ReturnsNotFound_WhenMissing));
        var controller = BuildController(context, 1);

        var result = await controller.GetCampaign("missing");

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<MapForgeController.ApiError>(notFound.Value);
        Assert.Equal("campaign_not_found", error.Code);
    }

    [Fact(DisplayName = "Get Campaign returns shared campaign with viewer role.")]
    public async Task GetCampaign_ReturnsSharedCampaignWithViewerRole()
    {
        var context = TestHelper.CreateContext(nameof(GetCampaign_ReturnsSharedCampaignWithViewerRole));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "viewer", Email = "viewer@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );

        var campaign = BuildCampaignEntity("cmp-1", 1, "Shared");
        context.MapCampaigns.Add(campaign);
        context.MapCampaignShares.Add(new MapForgeController.MapCampaignShare
        {
            CampaignId = campaign.Id,
            UserId = 2,
            Role = "viewer",
            SharedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            SharedByUserId = 1
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, 2);
        var result = await controller.GetCampaign(campaign.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MapForgeController.Campaign>(ok.Value);
        Assert.Equal("viewer", dto.AccessRole);
        Assert.False(dto.IsOwner);
    }

    [Fact(DisplayName = "Get Campaign returns not found when user has no access.")]
    public async Task GetCampaign_ReturnsNotFound_WhenNoAccess()
    {
        var context = TestHelper.CreateContext(nameof(GetCampaign_ReturnsNotFound_WhenNoAccess));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 3, Username = "other", Email = "other@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        context.MapCampaigns.Add(BuildCampaignEntity("cmp-2", 1));
        await context.SaveChangesAsync();

        var controller = BuildController(context, 3);
        var result = await controller.GetCampaign("cmp-2");

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<MapForgeController.ApiError>(notFound.Value);
        Assert.Equal("campaign_not_found", error.Code);
    }

    [Fact(DisplayName = "Save Campaign forbids editor when nodes are deleted.")]
    public async Task SaveCampaign_ForbidsEditor_WhenNodeDeleted()
    {
        var context = TestHelper.CreateContext(nameof(SaveCampaign_ForbidsEditor_WhenNodeDeleted));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "editor", Email = "editor@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        var campaign = BuildCampaignEntity("cmp-3", 1);
        context.MapCampaigns.Add(campaign);
        context.MapCampaignShares.Add(new MapForgeController.MapCampaignShare
        {
            CampaignId = campaign.Id,
            UserId = 2,
            Role = "editor",
            SharedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            SharedByUserId = 1
        });
        await context.SaveChangesAsync();

        var incomingNodes = new List<MapForgeController.FlowNode>
        {
            new()
            {
                Id = "n1",
                Position = new MapForgeController.Position { X = 0, Y = 0 },
                Data = new MapForgeController.NodeData { Label = "One", Type = "Location" }
            }
        };

        var controller = BuildController(context, 2);
        var result = await controller.SaveCampaign(campaign.Id, new MapForgeController.SaveCampaignRequest
        {
            Name = "Edited",
            Nodes = incomingNodes,
            Edges = new List<MapForgeController.FlowEdge>()
        });

        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, forbidden.StatusCode);
        var error = Assert.IsType<MapForgeController.ApiError>(forbidden.Value);
        Assert.Equal("node_delete_forbidden", error.Code);
    }

    [Fact(DisplayName = "Save Campaign allows editor when node count is preserved.")]
    public async Task SaveCampaign_AllowsEditor_WhenNodeCountPreserved()
    {
        var context = TestHelper.CreateContext(nameof(SaveCampaign_AllowsEditor_WhenNodeCountPreserved));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "editor", Email = "editor@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        var campaign = BuildCampaignEntity("cmp-4", 1, "Original");
        context.MapCampaigns.Add(campaign);
        context.MapCampaignShares.Add(new MapForgeController.MapCampaignShare
        {
            CampaignId = campaign.Id,
            UserId = 2,
            Role = "editor",
            SharedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            SharedByUserId = 1
        });
        await context.SaveChangesAsync();

        var incomingNodes = new List<MapForgeController.FlowNode>
        {
            new()
            {
                Id = "n1",
                Position = new MapForgeController.Position { X = 1, Y = 1 },
                Data = new MapForgeController.NodeData { Label = "One updated", Type = "Location" }
            },
            new()
            {
                Id = "n2",
                Position = new MapForgeController.Position { X = 2, Y = 2 },
                Data = new MapForgeController.NodeData { Label = "Two updated", Type = "Location" }
            }
        };

        var controller = BuildController(context, 2);
        var result = await controller.SaveCampaign(campaign.Id, new MapForgeController.SaveCampaignRequest
        {
            Name = "Edited",
            Nodes = incomingNodes,
            Edges = new List<MapForgeController.FlowEdge>()
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MapForgeController.Campaign>(ok.Value);
        Assert.Equal("Edited", dto.Name);
        Assert.Equal("editor", dto.AccessRole);
    }

    [Fact(DisplayName = "Delete Campaign removes campaign shares and invites.")]
    public async Task DeleteCampaign_RemovesCampaignAndRelations()
    {
        var context = TestHelper.CreateContext(nameof(DeleteCampaign_RemovesCampaignAndRelations));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "shared", Email = "shared@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        var campaign = BuildCampaignEntity("cmp-5", 1, "ToDelete");
        context.MapCampaigns.Add(campaign);
        context.MapCampaignShares.Add(new MapForgeController.MapCampaignShare
        {
            CampaignId = campaign.Id,
            UserId = 2,
            Role = "viewer",
            SharedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            SharedByUserId = 1
        });
        context.MapCampaignInvites.Add(new MapForgeController.MapCampaignInvite
        {
            CampaignId = campaign.Id,
            CreatedByUserId = 1,
            TargetUserId = 2,
            Role = "viewer",
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Status = "pending",
            IsLink = false
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.DeleteCampaign(campaign.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await context.MapCampaigns.AnyAsync());
        Assert.False(await context.MapCampaignShares.AnyAsync());
        Assert.False(await context.MapCampaignInvites.AnyAsync());
    }

    [Fact(DisplayName = "Delete Campaign returns not found for non-owner.")]
    public async Task DeleteCampaign_ReturnsNotFound_ForNonOwner()
    {
        var context = TestHelper.CreateContext(nameof(DeleteCampaign_ReturnsNotFound_ForNonOwner));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "other", Email = "other@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        var campaign = BuildCampaignEntity("cmp-6", 1);
        context.MapCampaigns.Add(campaign);
        await context.SaveChangesAsync();

        var controller = BuildController(context, 2);
        var result = await controller.DeleteCampaign(campaign.Id);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<MapForgeController.ApiError>(notFound.Value);
        Assert.Equal("campaign_not_found", error.Code);
    }

    [Fact(DisplayName = "Create Friend Invite rejects when users are not friends.")]
    public async Task CreateFriendInvite_RejectsWhenNotFriends()
    {
        var context = TestHelper.CreateContext(nameof(CreateFriendInvite_RejectsWhenNotFriends));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "target", Email = "target@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        var campaign = BuildCampaignEntity("cmp-7", 1);
        context.MapCampaigns.Add(campaign);
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.CreateFriendInvite(campaign.Id, new MapForgeController.CampaignInviteRequest
        {
            Username = "target",
            Role = "viewer"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<MapForgeController.ApiError>(badRequest.Value);
        Assert.Equal("not_friends", error.Code);
    }

    [Fact(DisplayName = "Create Friend Invite creates pending invite for accepted friend.")]
    public async Task CreateFriendInvite_CreatesPendingInvite_ForFriend()
    {
        var context = TestHelper.CreateContext(nameof(CreateFriendInvite_CreatesPendingInvite_ForFriend));
        var owner = new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var friend = new User { Id = 2, Username = "friend", Email = "friend@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(owner, friend);
        context.Friendships.Add(new Friendship
        {
            RequesterId = owner.Id,
            AddresseeId = friend.Id,
            Requester = owner,
            Addressee = friend,
            Status = FriendshipStatus.Accepted,
            RequestedAt = DateTime.UtcNow
        });
        var campaign = BuildCampaignEntity("cmp-8", owner.Id, "FriendsOnly");
        context.MapCampaigns.Add(campaign);
        await context.SaveChangesAsync();

        var controller = BuildController(context, owner.Id);
        var result = await controller.CreateFriendInvite(campaign.Id, new MapForgeController.CampaignInviteRequest
        {
            Username = friend.Username,
            Role = "editor"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MapForgeController.CampaignInviteDto>(ok.Value);
        Assert.Equal("editor", dto.Role);
        Assert.False(dto.IsLink);
        Assert.Equal("pending", dto.Status);
    }

    [Fact(DisplayName = "Accept Invite creates share and marks invite accepted.")]
    public async Task AcceptInvite_CreatesShareAndMarksAccepted()
    {
        var context = TestHelper.CreateContext(nameof(AcceptInvite_CreatesShareAndMarksAccepted));
        var owner = new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var invitee = new User { Id = 2, Username = "invitee", Email = "invitee@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(owner, invitee);
        var campaign = BuildCampaignEntity("cmp-9", owner.Id);
        context.MapCampaigns.Add(campaign);
        var invite = new MapForgeController.MapCampaignInvite
        {
            CampaignId = campaign.Id,
            CreatedByUserId = owner.Id,
            TargetUserId = invitee.Id,
            Role = "viewer",
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Status = "pending",
            IsLink = false
        };
        context.MapCampaignInvites.Add(invite);
        await context.SaveChangesAsync();

        var controller = BuildController(context, invitee.Id);
        var result = await controller.AcceptInvite(invite.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MapForgeController.CampaignShareDto>(ok.Value);
        Assert.Equal(invitee.Id, dto.UserId);

        var savedInvite = await context.MapCampaignInvites.FirstAsync(i => i.Id == invite.Id);
        Assert.Equal("accepted", savedInvite.Status);
        Assert.True(await context.MapCampaignShares.AnyAsync(s => s.CampaignId == campaign.Id && s.UserId == invitee.Id));
    }

    [Fact(DisplayName = "Claim Invite returns expired for old link token.")]
    public async Task ClaimInvite_ReturnsExpired_ForOldLinkToken()
    {
        var context = TestHelper.CreateContext(nameof(ClaimInvite_ReturnsExpired_ForOldLinkToken));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "claimer", Email = "claimer@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        var campaign = BuildCampaignEntity("cmp-10", 1);
        context.MapCampaigns.Add(campaign);
        context.MapCampaignInvites.Add(new MapForgeController.MapCampaignInvite
        {
            CampaignId = campaign.Id,
            CreatedByUserId = 1,
            TargetUserId = null,
            Role = "viewer",
            Token = "expired-token",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeMilliseconds(),
            Status = "pending",
            IsLink = true
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, 2);
        var result = await controller.ClaimInvite(new MapForgeController.CampaignInviteClaimRequest
        {
            Token = "expired-token"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<MapForgeController.ApiError>(badRequest.Value);
        Assert.Equal("invite_expired", error.Code);
    }

    [Fact(DisplayName = "Add Node returns not found when campaign is missing.")]
    public async Task AddNode_ReturnsNotFound_WhenCampaignMissing()
    {
        var context = TestHelper.CreateContext(nameof(AddNode_ReturnsNotFound_WhenCampaignMissing));
        var controller = BuildController(context, 1);

        var result = await controller.AddNode("missing", new MapForgeController.CreateNodeRequest
        {
            Data = new MapForgeController.NodeData { Label = "X", Type = "Location" }
        });

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<MapForgeController.ApiError>(notFound.Value);
        Assert.Equal("campaign_not_found", error.Code);
    }

    [Fact(DisplayName = "Add Node forbids viewer role.")]
    public async Task AddNode_ForbidsViewerRole()
    {
        var context = TestHelper.CreateContext(nameof(AddNode_ForbidsViewerRole));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "viewer", Email = "viewer@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        var campaign = BuildCampaignEntity("cmp-11", 1);
        context.MapCampaigns.Add(campaign);
        context.MapCampaignShares.Add(new MapForgeController.MapCampaignShare
        {
            CampaignId = campaign.Id,
            UserId = 2,
            Role = "viewer",
            SharedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            SharedByUserId = 1
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, 2);
        var result = await controller.AddNode(campaign.Id, new MapForgeController.CreateNodeRequest());

        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, forbidden.StatusCode);
        var error = Assert.IsType<MapForgeController.ApiError>(forbidden.Value);
        Assert.Equal("forbidden", error.Code);
    }

    [Fact(DisplayName = "Add Node persists new node for owner.")]
    public async Task AddNode_PersistsNode_ForOwner()
    {
        var context = TestHelper.CreateContext(nameof(AddNode_PersistsNode_ForOwner));
        context.Users.Add(new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow });
        var campaign = BuildCampaignEntity("cmp-12", 1);
        context.MapCampaigns.Add(campaign);
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.AddNode(campaign.Id, new MapForgeController.CreateNodeRequest
        {
            Id = "new-node",
            Position = new MapForgeController.Position { X = 100, Y = 200 },
            Data = new MapForgeController.NodeData { Label = "Added", Type = "NPC" }
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var node = Assert.IsType<MapForgeController.FlowNode>(ok.Value);
        Assert.Equal("new-node", node.Id);

        var saved = await context.MapCampaigns.FirstAsync(c => c.Id == campaign.Id);
        var nodes = JsonSerializer.Deserialize<List<MapForgeController.FlowNode>>(saved.NodesJson, ReadJsonOptions) ?? new();
        Assert.Contains(nodes, n => n.Id == "new-node");
    }

    [Fact(DisplayName = "Update Node returns not found when node id does not exist.")]
    public async Task UpdateNode_ReturnsNotFound_WhenNodeMissing()
    {
        var context = TestHelper.CreateContext(nameof(UpdateNode_ReturnsNotFound_WhenNodeMissing));
        context.Users.Add(new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow });
        var campaign = BuildCampaignEntity("cmp-13", 1);
        context.MapCampaigns.Add(campaign);
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.UpdateNode(campaign.Id, "missing-node", new MapForgeController.UpdateNodeRequest());

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<MapForgeController.ApiError>(notFound.Value);
        Assert.Equal("node_not_found", error.Code);
    }

    [Fact(DisplayName = "Delete Node removes node and connected edges.")]
    public async Task DeleteNode_RemovesNodeAndConnectedEdges()
    {
        var context = TestHelper.CreateContext(nameof(DeleteNode_RemovesNodeAndConnectedEdges));
        context.Users.Add(new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow });
        var campaign = BuildCampaignEntity("cmp-14", 1);
        context.MapCampaigns.Add(campaign);
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.DeleteNode(campaign.Id, "n1");

        Assert.IsType<NoContentResult>(result);
        var saved = await context.MapCampaigns.FirstAsync(c => c.Id == campaign.Id);
        var nodes = JsonSerializer.Deserialize<List<MapForgeController.FlowNode>>(saved.NodesJson, ReadJsonOptions) ?? new();
        var edges = JsonSerializer.Deserialize<List<MapForgeController.FlowEdge>>(saved.EdgesJson, ReadJsonOptions) ?? new();
        Assert.DoesNotContain(nodes, n => n.Id == "n1");
        Assert.DoesNotContain(edges, e => e.Source == "n1" || e.Target == "n1");
    }

    [Fact(DisplayName = "Add Edge rejects when source or target node is invalid.")]
    public async Task AddEdge_RejectsInvalidNodes()
    {
        var context = TestHelper.CreateContext(nameof(AddEdge_RejectsInvalidNodes));
        context.Users.Add(new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow });
        var campaign = BuildCampaignEntity("cmp-15", 1);
        context.MapCampaigns.Add(campaign);
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.AddEdge(campaign.Id, new MapForgeController.CreateEdgeRequest
        {
            Source = "missing",
            Target = "n2"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<MapForgeController.ApiError>(badRequest.Value);
        Assert.Equal("invalid_edge", error.Code);
    }

    [Fact(DisplayName = "Add Edge persists edge for valid source and target.")]
    public async Task AddEdge_PersistsEdge_WhenValid()
    {
        var context = TestHelper.CreateContext(nameof(AddEdge_PersistsEdge_WhenValid));
        context.Users.Add(new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow });
        var campaign = BuildCampaignEntity("cmp-16", 1);
        context.MapCampaigns.Add(campaign);
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.AddEdge(campaign.Id, new MapForgeController.CreateEdgeRequest
        {
            Id = "n2-n1-custom",
            Source = "n2",
            Target = "n1",
            Type = "straight"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var edge = Assert.IsType<MapForgeController.FlowEdge>(ok.Value);
        Assert.Equal("n2-n1-custom", edge.Id);

        var saved = await context.MapCampaigns.FirstAsync(c => c.Id == campaign.Id);
        var edges = JsonSerializer.Deserialize<List<MapForgeController.FlowEdge>>(saved.EdgesJson, ReadJsonOptions) ?? new();
        Assert.Contains(edges, e => e.Id == "n2-n1-custom");
    }

    [Fact(DisplayName = "Delete Edge returns not found for unknown edge id.")]
    public async Task DeleteEdge_ReturnsNotFound_WhenEdgeMissing()
    {
        var context = TestHelper.CreateContext(nameof(DeleteEdge_ReturnsNotFound_WhenEdgeMissing));
        context.Users.Add(new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow });
        var campaign = BuildCampaignEntity("cmp-17", 1);
        context.MapCampaigns.Add(campaign);
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.DeleteEdge(campaign.Id, "missing-edge");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<MapForgeController.ApiError>(notFound.Value);
        Assert.Equal("edge_not_found", error.Code);
    }

    [Fact(DisplayName = "Get Shares returns sorted share list for owner.")]
    public async Task GetShares_ReturnsSortedShares_ForOwner()
    {
        var context = TestHelper.CreateContext(nameof(GetShares_ReturnsSortedShares_ForOwner));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "zeta", Email = "zeta@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 3, Username = "alpha", Email = "alpha@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        var campaign = BuildCampaignEntity("cmp-18", 1);
        context.MapCampaigns.Add(campaign);
        context.MapCampaignShares.AddRange(
            new MapForgeController.MapCampaignShare { CampaignId = campaign.Id, UserId = 2, Role = "viewer", SharedAt = 1, SharedByUserId = 1 },
            new MapForgeController.MapCampaignShare { CampaignId = campaign.Id, UserId = 3, Role = "editor", SharedAt = 2, SharedByUserId = 1 }
        );
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.GetShares(campaign.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var shares = Assert.IsAssignableFrom<List<MapForgeController.CampaignShareDto>>(ok.Value);
        Assert.Equal(2, shares.Count);
        Assert.Equal("alpha", shares[0].Username);
        Assert.Equal("editor", shares[0].Role);
    }

    [Fact(DisplayName = "Update Share changes role with normalization.")]
    public async Task UpdateShare_ChangesRole_WithNormalization()
    {
        var context = TestHelper.CreateContext(nameof(UpdateShare_ChangesRole_WithNormalization));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "shared", Email = "shared@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        var campaign = BuildCampaignEntity("cmp-19", 1);
        context.MapCampaigns.Add(campaign);
        context.MapCampaignShares.Add(new MapForgeController.MapCampaignShare
        {
            CampaignId = campaign.Id,
            UserId = 2,
            Role = "viewer",
            SharedAt = 1,
            SharedByUserId = 1
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.UpdateShare(campaign.Id, 2, new MapForgeController.CampaignShareRoleRequest
        {
            Role = "EDITOR"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MapForgeController.CampaignShareDto>(ok.Value);
        Assert.Equal("editor", dto.Role);
    }

    [Fact(DisplayName = "Delete Share removes share for owner.")]
    public async Task DeleteShare_RemovesShare_ForOwner()
    {
        var context = TestHelper.CreateContext(nameof(DeleteShare_RemovesShare_ForOwner));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "shared", Email = "shared@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        var campaign = BuildCampaignEntity("cmp-20", 1);
        context.MapCampaigns.Add(campaign);
        context.MapCampaignShares.Add(new MapForgeController.MapCampaignShare
        {
            CampaignId = campaign.Id,
            UserId = 2,
            Role = "viewer",
            SharedAt = 1,
            SharedByUserId = 1
        });
        await context.SaveChangesAsync();

        var controller = BuildController(context, 1);
        var result = await controller.DeleteShare(campaign.Id, 2);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await context.MapCampaignShares.AnyAsync());
    }

    [Fact(DisplayName = "Get My Invites returns only pending direct invites for user.")]
    public async Task GetMyInvites_ReturnsOnlyPendingDirectInvites()
    {
        var context = TestHelper.CreateContext(nameof(GetMyInvites_ReturnsOnlyPendingDirectInvites));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "me", Email = "me@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        var campaign = BuildCampaignEntity("cmp-21", 1, "InvitedCampaign");
        context.MapCampaigns.Add(campaign);
        context.MapCampaignInvites.AddRange(
            new MapForgeController.MapCampaignInvite
            {
                CampaignId = campaign.Id,
                CreatedByUserId = 1,
                TargetUserId = 2,
                Role = "viewer",
                CreatedAt = 1,
                Status = "pending",
                IsLink = false
            },
            new MapForgeController.MapCampaignInvite
            {
                CampaignId = campaign.Id,
                CreatedByUserId = 1,
                TargetUserId = 2,
                Role = "viewer",
                CreatedAt = 2,
                Status = "accepted",
                IsLink = false
            }
        );
        await context.SaveChangesAsync();

        var controller = BuildController(context, 2);
        var result = await controller.GetMyInvites();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<List<MapForgeController.CampaignInviteDto>>(ok.Value);
        Assert.Single(list);
        Assert.Equal("pending", list[0].Status);
        Assert.Equal("InvitedCampaign", list[0].CampaignName);
    }

    [Fact(DisplayName = "Decline Invite marks invite declined and returns no content.")]
    public async Task DeclineInvite_MarksDeclined_ReturnsNoContent()
    {
        var context = TestHelper.CreateContext(nameof(DeclineInvite_MarksDeclined_ReturnsNoContent));
        context.Users.AddRange(
            new User { Id = 1, Username = "owner", Email = "owner@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "invitee", Email = "invitee@dnd.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow }
        );
        var campaign = BuildCampaignEntity("cmp-22", 1);
        context.MapCampaigns.Add(campaign);
        var invite = new MapForgeController.MapCampaignInvite
        {
            CampaignId = campaign.Id,
            CreatedByUserId = 1,
            TargetUserId = 2,
            Role = "viewer",
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Status = "pending",
            IsLink = false
        };
        context.MapCampaignInvites.Add(invite);
        await context.SaveChangesAsync();

        var controller = BuildController(context, 2);
        var result = await controller.DeclineInvite(invite.Id);

        Assert.IsType<NoContentResult>(result);
        var saved = await context.MapCampaignInvites.FirstAsync(i => i.Id == invite.Id);
        Assert.Equal("declined", saved.Status);
        Assert.Equal(2, saved.AcceptedByUserId);
    }
}
