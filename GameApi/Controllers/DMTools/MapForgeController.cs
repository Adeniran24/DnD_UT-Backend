using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameApi.Data;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/mapforge")]
    [Authorize]
    public class MapForgeController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MapForgeController(AppDbContext db)
        {
            _db = db;
        }

        // ===== JSON options (ReactFlow kompatibilis) =====
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // ===== Helpers =====
        private static string CampaignId() => $"campaign-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        private static string NodeId() => $"node-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Random.Shared.Next(10000, 99999)}";

        private int GetCurrentUserId()
        {
            // A te JwtService-edtől függ, de ezek a leggyakoribbak:
            var idStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("id") ??
                User.FindFirstValue("userId") ??
                User.FindFirstValue("sub");

            if (!int.TryParse(idStr, out var userId))
                throw new UnauthorizedAccessException("Missing/invalid user id claim in JWT.");

            return userId;
        }

        private static string ToJson<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

        private static T FromJson<T>(string json, T fallback)
        {
            try
            {
                var val = JsonSerializer.Deserialize<T>(json, JsonOptions);
                return val is null ? fallback : val;
            }
            catch
            {
                return fallback;
            }
        }

        // ===== Image upload helpers =====
        private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        // NOTE: ha nagyobbat akarsz, emeld meg (frontendben is jó jelezni)
        private const long MaxImageBytes = 5 * 1024 * 1024; // 5MB

        private string UploadRootPath => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        private async Task<string> SaveImageAsync(IFormFile file, string subfolder, string fileNameBase)
        {
            if (file is null || file.Length == 0)
                throw new ValidationException("No file uploaded.");

            if (file.Length > MaxImageBytes)
                throw new ValidationException("Image too large. Max 5MB.");

            if (!AllowedImageTypes.Contains(file.ContentType))
                throw new ValidationException("Invalid image type. Allowed: jpg, png, webp.");

            Directory.CreateDirectory(UploadRootPath);
            var folderPath = Path.Combine(UploadRootPath, subfolder);
            Directory.CreateDirectory(folderPath);

            var ext = file.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".img"
            };

            // Biztonságos filename (ne a user file nevét használd)
            var safeName = $"{fileNameBase}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Random.Shared.Next(1000, 9999)}{ext}";
            var fullPath = Path.Combine(folderPath, safeName);

            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);

            // Relatív URL: /uploads/...
            // Frontenden célszerű API_BASE + imageUrl, ha nem abszolút.
            return $"/uploads/{subfolder}/{safeName}";
        }

        private static Campaign CreateStarterCampaign(string name)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var n1 = new FlowNode
            {
                Id = NodeId(),
                Position = new Position { X = 0, Y = 0 },
                Data = new NodeData
                {
                    Label = "Starting Town",
                    Type = "Location",
                    Tags = new List<string> { "home" },
                    Notes = "",
                    ImageUrl = null,
                    Statblock = null
                }
            };
            var n2 = new FlowNode
            {
                Id = NodeId(),
                Position = new Position { X = 260, Y = 120 },
                Data = new NodeData
                {
                    Label = "Local Patron",
                    Type = "NPC",
                    Tags = new List<string> { "quest" },
                    Notes = "",
                    ImageUrl = null,
                    Statblock = null
                }
            };
            var n3 = new FlowNode
            {
                Id = NodeId(),
                Position = new Position { X = -220, Y = 180 },
                Data = new NodeData
                {
                    Label = "Old Ruins",
                    Type = "Location",
                    Tags = new List<string> { "danger" },
                    Notes = "",
                    ImageUrl = null,
                    Statblock = null
                }
            };

            var e1 = new FlowEdge
            {
                Id = $"{n1.Id}-{n2.Id}",
                Source = n1.Id,
                Target = n2.Id,
                Type = "smoothstep"
            };
            var e2 = new FlowEdge
            {
                Id = $"{n1.Id}-{n3.Id}",
                Source = n1.Id,
                Target = n3.Id,
                Type = "smoothstep"
            };

            return new Campaign
            {
                Id = CampaignId(),
                Name = name.Trim(),
                Nodes = new List<FlowNode> { n1, n2, n3 },
                Edges = new List<FlowEdge> { e1, e2 },
                UpdatedAt = now,
                CoverImageUrl = null
            };
        }

        // =========================================================
        // Campaign endpoints
        // =========================================================

        // GET /api/mapforge/campaigns  (csak saját)
        [HttpGet("campaigns")]
        public async Task<ActionResult<List<CampaignSummary>>> GetCampaigns()
        {
            var userId = GetCurrentUserId();

            // Lista (AsNoTracking gyorsabb)
            var entities = await _db.MapCampaigns
                .AsNoTracking()
                .Where(c => c.OwnerUserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            // Node/Edge count JSON parse-ból
            var list = entities.Select(e =>
            {
                var nodes = FromJson(e.NodesJson ?? "[]", new List<FlowNode>());
                var edges = FromJson(e.EdgesJson ?? "[]", new List<FlowEdge>());

                return new CampaignSummary
                {
                    Id = e.Id,
                    Name = e.Name,
                    UpdatedAt = e.UpdatedAt,
                    NodeCount = nodes.Count,
                    EdgeCount = edges.Count,
                    CoverImageUrl = e.CoverImageUrl
                };
            }).ToList();

            return Ok(list);
        }

        // GET /api/mapforge/campaigns/{id}  (csak saját)
        [HttpGet("campaigns/{id}")]
        public async Task<ActionResult<Campaign>> GetCampaign(string id)
        {
            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            return Ok(new Campaign
            {
                Id = entity.Id,
                Name = entity.Name,
                Nodes = FromJson(entity.NodesJson ?? "[]", new List<FlowNode>()),
                Edges = FromJson(entity.EdgesJson ?? "[]", new List<FlowEdge>()),
                UpdatedAt = entity.UpdatedAt,
                CoverImageUrl = entity.CoverImageUrl
            });
        }

        // POST /api/mapforge/campaigns
        // Body: { "name": "My Campaign", "seedStarter": true }
        [HttpPost("campaigns")]
        public async Task<ActionResult<Campaign>> CreateCampaign([FromBody] CreateCampaignRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            var name = req.Name.Trim();

            var campaign = req.SeedStarter
                ? CreateStarterCampaign(name)
                : new Campaign
                {
                    Id = CampaignId(),
                    Name = name,
                    Nodes = new List<FlowNode>(),
                    Edges = new List<FlowEdge>(),
                    UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    CoverImageUrl = null
                };

            var entity = new MapCampaign
            {
                Id = campaign.Id,
                OwnerUserId = userId,
                Name = campaign.Name,
                NodesJson = ToJson(campaign.Nodes),
                EdgesJson = ToJson(campaign.Edges),
                UpdatedAt = campaign.UpdatedAt,
                CoverImageUrl = campaign.CoverImageUrl
            };

            _db.MapCampaigns.Add(entity);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
        }

        // PUT /api/mapforge/campaigns/{id}
        // Body: { "name": "...", "nodes": [...], "edges": [...] }
        [HttpPut("campaigns/{id}")]
        public async Task<ActionResult<Campaign>> SaveCampaign(string id, [FromBody] SaveCampaignRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            entity.Name = req.Name.Trim();
            entity.NodesJson = ToJson(req.Nodes ?? new List<FlowNode>());
            entity.EdgesJson = ToJson(req.Edges ?? new List<FlowEdge>());
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            await _db.SaveChangesAsync();

            return Ok(new Campaign
            {
                Id = entity.Id,
                Name = entity.Name,
                Nodes = FromJson(entity.NodesJson ?? "[]", new List<FlowNode>()),
                Edges = FromJson(entity.EdgesJson ?? "[]", new List<FlowEdge>()),
                UpdatedAt = entity.UpdatedAt,
                CoverImageUrl = entity.CoverImageUrl
            });
        }

        // PATCH /api/mapforge/campaigns/{id}/name
        [HttpPatch("campaigns/{id}/name")]
        public async Task<ActionResult<Campaign>> RenameCampaign(string id, [FromBody] RenameCampaignRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            entity.Name = req.Name.Trim();
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();

            return Ok(new Campaign
            {
                Id = entity.Id,
                Name = entity.Name,
                Nodes = FromJson(entity.NodesJson ?? "[]", new List<FlowNode>()),
                Edges = FromJson(entity.EdgesJson ?? "[]", new List<FlowEdge>()),
                UpdatedAt = entity.UpdatedAt,
                CoverImageUrl = entity.CoverImageUrl
            });
        }

        // DELETE /api/mapforge/campaigns/{id}
        [HttpDelete("campaigns/{id}")]
        public async Task<IActionResult> DeleteCampaign(string id)
        {
            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            _db.MapCampaigns.Remove(entity);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // =========================================================
        // Campaign cover image upload
        // =========================================================

        // POST /api/mapforge/campaigns/{id}/cover
        // multipart/form-data: file
        [HttpPost("campaigns/{id}/cover")]
        [RequestSizeLimit(MaxImageBytes)]
        public async Task<ActionResult<object>> UploadCampaignCover(string id, [FromForm] IFormFile file)
        {
            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            try
            {
                var url = await SaveImageAsync(file, $"campaigns/{entity.Id}", "cover");
                entity.CoverImageUrl = url;
                entity.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await _db.SaveChangesAsync();

                return Ok(new { coverImageUrl = entity.CoverImageUrl, updatedAt = entity.UpdatedAt });
            }
            catch (ValidationException ve)
            {
                return BadRequest(new ApiError("invalid_image", ve.Message));
            }
        }

        // =========================================================
        // Node endpoints (DB-ben is működik: JSON read-modify-write)
        // =========================================================

        [HttpPost("campaigns/{id}/nodes")]
        public async Task<ActionResult<FlowNode>> AddNode(string id, [FromBody] CreateNodeRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var nodes = FromJson(entity.NodesJson ?? "[]", new List<FlowNode>());

            var node = new FlowNode
            {
                Id = string.IsNullOrWhiteSpace(req.Id) ? NodeId() : req.Id!,
                Position = req.Position ?? new Position { X = 0, Y = 0 },
                Data = req.Data ?? new NodeData
                {
                    Label = "New Node",
                    Type = "Location",
                    Tags = new List<string>(),
                    Notes = "",
                    ImageUrl = null,
                    Statblock = null
                }
            };

            // Ha a client nem küldte, legyen biztosan default
            node.Data ??= new NodeData();
            node.Data.ImageUrl ??= null;
            // Statblock maradhat null (Monster esetén majd UI küldi)

            nodes.Add(node);

            entity.NodesJson = ToJson(nodes);
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();

            return Ok(node);
        }

        [HttpPut("campaigns/{id}/nodes/{nodeId}")]
        public async Task<ActionResult<FlowNode>> UpdateNode(string id, string nodeId, [FromBody] UpdateNodeRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var nodes = FromJson(entity.NodesJson ?? "[]", new List<FlowNode>());
            var node = nodes.FirstOrDefault(n => n.Id == nodeId);

            if (node is null)
                return NotFound(new ApiError("node_not_found", "Node not found."));

            if (req.Position is not null) node.Position = req.Position;

            if (req.Data is not null)
            {
                node.Data ??= new NodeData();

                node.Data.Label = req.Data.Label ?? node.Data.Label;
                node.Data.Type = req.Data.Type ?? node.Data.Type;
                node.Data.Notes = req.Data.Notes ?? node.Data.Notes;

                if (req.Data.Tags is not null)
                    node.Data.Tags = req.Data.Tags;

                // ÚJ mezők
                if (req.Data.ImageUrl is not null)
                    node.Data.ImageUrl = req.Data.ImageUrl;

                if (req.Data.Statblock is not null)
                    node.Data.Statblock = req.Data.Statblock;
            }

            entity.NodesJson = ToJson(nodes);
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();

            return Ok(node);
        }

        [HttpDelete("campaigns/{id}/nodes/{nodeId}")]
        public async Task<IActionResult> DeleteNode(string id, string nodeId)
        {
            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var nodes = FromJson(entity.NodesJson ?? "[]", new List<FlowNode>());
            var edges = FromJson(entity.EdgesJson ?? "[]", new List<FlowEdge>());

            var removed = nodes.RemoveAll(n => n.Id == nodeId);
            if (removed == 0)
                return NotFound(new ApiError("node_not_found", "Node not found."));

            edges.RemoveAll(e => e.Source == nodeId || e.Target == nodeId);

            entity.NodesJson = ToJson(nodes);
            entity.EdgesJson = ToJson(edges);
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // =========================================================
        // Node image upload/remove (kép a node-hoz)
        // =========================================================

        // POST /api/mapforge/campaigns/{id}/nodes/{nodeId}/image
        // multipart/form-data: file
        [HttpPost("campaigns/{id}/nodes/{nodeId}/image")]
        [RequestSizeLimit(MaxImageBytes)]
        public async Task<ActionResult<object>> UploadNodeImage(string id, string nodeId, [FromForm] IFormFile file)
        {
            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var nodes = FromJson(entity.NodesJson ?? "[]", new List<FlowNode>());
            var node = nodes.FirstOrDefault(n => n.Id == nodeId);

            if (node is null)
                return NotFound(new ApiError("node_not_found", "Node not found."));

            try
            {
                var url = await SaveImageAsync(file, $"campaigns/{entity.Id}/nodes", nodeId);
                node.Data ??= new NodeData();
                node.Data.ImageUrl = url;

                entity.NodesJson = ToJson(nodes);
                entity.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await _db.SaveChangesAsync();

                return Ok(new { imageUrl = url, updatedAt = entity.UpdatedAt });
            }
            catch (ValidationException ve)
            {
                return BadRequest(new ApiError("invalid_image", ve.Message));
            }
        }

        // DELETE /api/mapforge/campaigns/{id}/nodes/{nodeId}/image
        [HttpDelete("campaigns/{id}/nodes/{nodeId}/image")]
        public async Task<IActionResult> RemoveNodeImage(string id, string nodeId)
        {
            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var nodes = FromJson(entity.NodesJson ?? "[]", new List<FlowNode>());
            var node = nodes.FirstOrDefault(n => n.Id == nodeId);

            if (node is null)
                return NotFound(new ApiError("node_not_found", "Node not found."));

            node.Data ??= new NodeData();
            node.Data.ImageUrl = null;

            entity.NodesJson = ToJson(nodes);
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // =========================================================
        // Edge endpoints
        // =========================================================

        [HttpPost("campaigns/{id}/edges")]
        public async Task<ActionResult<FlowEdge>> AddEdge(string id, [FromBody] CreateEdgeRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var nodes = FromJson(entity.NodesJson ?? "[]", new List<FlowNode>());

            if (!nodes.Any(n => n.Id == req.Source) || !nodes.Any(n => n.Id == req.Target))
                return BadRequest(new ApiError("invalid_edge", "Edge source/target must reference existing nodes."));

            var edges = FromJson(entity.EdgesJson ?? "[]", new List<FlowEdge>());

            var edge = new FlowEdge
            {
                Id = string.IsNullOrWhiteSpace(req.Id) ? $"{req.Source}-{req.Target}" : req.Id!,
                Source = req.Source,
                Target = req.Target,
                Type = string.IsNullOrWhiteSpace(req.Type) ? "smoothstep" : req.Type
            };

            edges.Add(edge);

            entity.EdgesJson = ToJson(edges);
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();

            return Ok(edge);
        }

        [HttpDelete("campaigns/{id}/edges/{edgeId}")]
        public async Task<IActionResult> DeleteEdge(string id, string edgeId)
        {
            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var edges = FromJson(entity.EdgesJson ?? "[]", new List<FlowEdge>());

            var removed = edges.RemoveAll(e => e.Id == edgeId);
            if (removed == 0)
                return NotFound(new ApiError("edge_not_found", "Edge not found."));

            entity.EdgesJson = ToJson(edges);
            entity.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // =========================================================
        // Models + DB entity (same file)
        // =========================================================

        // DB entity (EF Core) -> MapCampaigns tábla
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

            // ÚJ: kampány borítókép URL (relatív: /uploads/...)
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

            // ÚJ
            public string? CoverImageUrl { get; set; }
        }

        public sealed class CampaignSummary
        {
            public string Id { get; set; } = default!;
            public string Name { get; set; } = default!;
            public long UpdatedAt { get; set; }
            public int NodeCount { get; set; }
            public int EdgeCount { get; set; }

            // ÚJ
            public string? CoverImageUrl { get; set; }
        }

        public sealed class FlowNode
        {
            [Required]
            public string Id { get; set; } = default!;

            // ReactFlow node "type" (render) mező optional; ha kell, add hozzá később.
            // Most a te frontended használhatja default-ként.
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
    }
}
// End of file Controllers/DMTools/MapForgeController.cs