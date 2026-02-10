using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameApi.Controllers
{
    public partial class MapForgeController
    {
        // =========================================================
        // Models + DB entity (split into partial)
        // =========================================================

        // DB entity (EF Core) -> MapCampaigns table
        public sealed class MapCampaign
        {
            [Key]
            [MaxLength(64)]
            public string Id { get; set; } = default!;

            [Required, MaxLength(80)]
            public string Name { get; set; } = default!;

            [Required]
            public string NodesJson { get; set; } = "[]";

            [Required]
            public string EdgesJson { get; set; } = "[]";

            public long UpdatedAt { get; set; }

            public int OwnerUserId { get; set; }

            // New: campaign cover image URL (relative: /uploads/...)
            public string? CoverImageUrl { get; set; }
        }

        // API model
        public sealed class Campaign
        {
            [Required]
            public string Id { get; set; } = default!;

            [Required, MinLength(1), MaxLength(80)]
            public string Name { get; set; } = default!;

            public List<FlowNode> Nodes { get; set; } = new();
            public List<FlowEdge> Edges { get; set; } = new();
            public long UpdatedAt { get; set; }

            // New
            public string? CoverImageUrl { get; set; }

            public string AccessRole { get; set; } = AccessOwner;
            public bool IsOwner { get; set; } = true;
        }

        public sealed class CampaignSummary
        {
            public string Id { get; set; } = default!;
            public string Name { get; set; } = default!;
            public long UpdatedAt { get; set; }
            public int NodeCount { get; set; }
            public int EdgeCount { get; set; }

            // New
            public string? CoverImageUrl { get; set; }
            public string AccessRole { get; set; } = AccessOwner;
            public bool IsOwner { get; set; } = true;
        }

        public sealed class MapCampaignShare
        {
            [Required, MaxLength(64)]
            public string CampaignId { get; set; } = default!;

            public int UserId { get; set; }

            [Required, MaxLength(10)]
            public string Role { get; set; } = AccessViewer;

            public long SharedAt { get; set; }
            public int SharedByUserId { get; set; }
        }

        public sealed class MapCampaignInvite
        {
            [Key]
            public int Id { get; set; }

            [Required, MaxLength(64)]
            public string CampaignId { get; set; } = default!;

            public int CreatedByUserId { get; set; }
            public int? TargetUserId { get; set; }

            [Required, MaxLength(10)]
            public string Role { get; set; } = AccessViewer;

            [MaxLength(64)]
            public string? Token { get; set; }

            public long CreatedAt { get; set; }
            public long? ExpiresAt { get; set; }
            public long? AcceptedAt { get; set; }
            public int? AcceptedByUserId { get; set; }

            [Required, MaxLength(12)]
            public string Status { get; set; } = InviteStatusPending;

            public bool IsLink { get; set; }
        }

        public sealed class FlowNode
        {
            [Required]
            public string Id { get; set; } = default!;

            // ReactFlow node "type" (render) mező optional; ha kell, add hozzá később.
            // Most a te frontended használhatja defaultként.
            [JsonPropertyName("type")]
            public string? RenderType { get; set; }

            [Required]
            public Position Position { get; set; } = new();

            [Required]
            public NodeData Data { get; set; } = new();
        }

        public sealed class Position
        {
            [JsonPropertyName("x")]
            public double X { get; set; }

            [JsonPropertyName("y")]
            public double Y { get; set; }
        }

        public sealed class NodeData
        {
            [JsonPropertyName("label")]
            public string Label { get; set; } = "New Node";

            [JsonPropertyName("type")]
            public string Type { get; set; } = "Location";

            [JsonPropertyName("tags")]
            public List<string> Tags { get; set; } = new();

            [JsonPropertyName("notes")]
            public string Notes { get; set; } = "";

            // ÚJ: node kép
            [JsonPropertyName("imageUrl")]
            public string? ImageUrl { get; set; }

            // ÚJ: Monster statblock (tetszőleges JSON)
            [JsonPropertyName("statblock")]
            public JsonElement? Statblock { get; set; }
        }

        public sealed class FlowEdge
        {
            [Required]
            public string Id { get; set; } = default!;

            [Required]
            public string Source { get; set; } = default!;

            [Required]
            public string Target { get; set; } = default!;

            public string Type { get; set; } = "smoothstep";
        }

        public sealed class CreateCampaignRequest
        {
            [Required, MinLength(1), MaxLength(80)]
            public string Name { get; set; } = default!;
            public bool SeedStarter { get; set; } = true;
        }

        public sealed class SaveCampaignRequest
        {
            [Required, MinLength(1), MaxLength(80)]
            public string Name { get; set; } = default!;
            public List<FlowNode>? Nodes { get; set; }
            public List<FlowEdge>? Edges { get; set; }
        }

        public sealed class RenameCampaignRequest
        {
            [Required, MinLength(1), MaxLength(80)]
            public string Name { get; set; } = default!;
        }

        public sealed class CreateNodeRequest
        {
            public string? Id { get; set; }
            public Position? Position { get; set; }
            public NodeData? Data { get; set; }
        }

        public sealed class UpdateNodeRequest
        {
            public Position? Position { get; set; }
            public PartialNodeData? Data { get; set; }
        }

        public sealed class PartialNodeData
        {
            public string? Label { get; set; }
            public string? Type { get; set; }
            public List<string>? Tags { get; set; }
            public string? Notes { get; set; }

            // ÚJ
            public string? ImageUrl { get; set; }
            public JsonElement? Statblock { get; set; }
        }

        public sealed class CreateEdgeRequest
        {
            public string? Id { get; set; }

            [Required]
            public string Source { get; set; } = default!;

            [Required]
            public string Target { get; set; } = default!;

            public string? Type { get; set; } = "smoothstep";
        }

        public sealed class ApiError
        {
            public ApiError(string code, string message)
            {
                Code = code;
                Message = message;
            }
            public string Code { get; set; }
            public string Message { get; set; }
        }

        public sealed class CampaignShareDto
        {
            public int UserId { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Role { get; set; } = AccessViewer;
        }

        public sealed class CampaignShareRequest
        {
            [Required, MinLength(1), MaxLength(40)]
            public string Username { get; set; } = string.Empty;

            [Required, MinLength(4), MaxLength(10)]
            public string Role { get; set; } = AccessViewer;
        }

        public sealed class CampaignShareRoleRequest
        {
            [Required, MinLength(4), MaxLength(10)]
            public string Role { get; set; } = AccessViewer;
        }

        public sealed class CampaignInviteRequest
        {
            [Required, MinLength(1), MaxLength(40)]
            public string Username { get; set; } = string.Empty;

            [Required, MinLength(4), MaxLength(10)]
            public string Role { get; set; } = AccessViewer;
        }

        public sealed class CampaignInviteLinkRequest
        {
            [Required, MinLength(4), MaxLength(10)]
            public string Role { get; set; } = AccessViewer;
        }

        public sealed class CampaignInviteClaimRequest
        {
            [Required, MinLength(6), MaxLength(80)]
            public string Token { get; set; } = string.Empty;
        }

        public sealed class CampaignInviteDto
        {
            public int Id { get; set; }
            public string CampaignId { get; set; } = string.Empty;
            public string CampaignName { get; set; } = string.Empty;
            public string Role { get; set; } = AccessViewer;
            public long CreatedAt { get; set; }
            public long? ExpiresAt { get; set; }
            public string Status { get; set; } = InviteStatusPending;
            public bool IsLink { get; set; }
        }

        public sealed class CampaignInviteLinkDto
        {
            public string Token { get; set; } = string.Empty;
            public string Role { get; set; } = AccessViewer;
            public long ExpiresAt { get; set; }
        }
    }
}
