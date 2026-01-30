using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameApi.Data;
using GameApi.Models;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/mapforge")]
    [Authorize]
    public class MapForgeController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public MapForgeController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ===== JSON options (ReactFlow kompatibilis) =====
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // ===== Helpers =====
        private static string CampaignId() => $"campaign-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        private static string NodeId() => $"node-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Random.Shared.Next(10000, 99999)}";
        private const string AccessOwner = "owner";
        private const string AccessEditor = "editor";
        private const string AccessViewer = "viewer";
        private const string InviteStatusPending = "pending";
        private const string InviteStatusAccepted = "accepted";
        private const string InviteStatusDeclined = "declined";
        private const string InviteStatusExpired = "expired";

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

        private static string NormalizeAccessRole(string? role)
        {
            var normalized = (role ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                AccessEditor => AccessEditor,
                AccessViewer => AccessViewer,
                _ => AccessViewer
            };
        }

        private static bool CanEdit(string role) => role == AccessOwner || role == AccessEditor;

        private async Task<string?> GetAccessRoleAsync(MapCampaign campaign, int userId)
        {
            if (campaign.OwnerUserId == userId) return AccessOwner;

            var share = await _db.MapCampaignShares
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CampaignId == campaign.Id && s.UserId == userId);

            return share is null ? null : NormalizeAccessRole(share.Role);
        }

        private async Task<bool> IsFriendAsync(int userId, int targetUserId)
        {
            return await _db.Friendships
                .AsNoTracking()
                .AnyAsync(f =>
                    ((f.RequesterId == userId && f.AddresseeId == targetUserId) ||
                     (f.RequesterId == targetUserId && f.AddresseeId == userId)) &&
                    f.Status == FriendshipStatus.Accepted);
        }

        private static string GenerateInviteToken()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static bool IsInviteExpired(long? expiresAt)
        {
            if (!expiresAt.HasValue) return false;
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > expiresAt.Value;
        }

        private static bool HasDeletedNodes(List<FlowNode> existingNodes, List<FlowNode> incomingNodes)
        {
            if (existingNodes.Count == 0) return false;
            var incomingIds = new HashSet<string>(incomingNodes.Select(n => n.Id));
            return existingNodes.Any(n => !incomingIds.Contains(n.Id));
        }

        // ===== Image upload helpers =====
        private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        // NOTE: ha nagyobbat akarsz, emeld meg (frontendben is jó jelezni)
        private const long MaxImageBytes = 5 * 1024 * 1024; // 5MB

        private string UploadRootPath =>
            Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");

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
            var ownedEntities = await _db.MapCampaigns
                .AsNoTracking()
                .Where(c => c.OwnerUserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            // Node/Edge count JSON parsebol
            var ownedList = ownedEntities.Select(e =>
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
                    CoverImageUrl = e.CoverImageUrl,
                    AccessRole = AccessOwner,
                    IsOwner = true
                };
            }).ToList();

            var sharedEntries = await (
                from share in _db.MapCampaignShares.AsNoTracking()
                join campaign in _db.MapCampaigns.AsNoTracking() on share.CampaignId equals campaign.Id
                where share.UserId == userId && campaign.OwnerUserId != userId
                select new { Campaign = campaign, share.Role }
            ).ToListAsync();

            var sharedList = sharedEntries.Select(entry =>
            {
                var nodes = FromJson(entry.Campaign.NodesJson ?? "[]", new List<FlowNode>());
                var edges = FromJson(entry.Campaign.EdgesJson ?? "[]", new List<FlowEdge>());

                return new CampaignSummary
                {
                    Id = entry.Campaign.Id,
                    Name = entry.Campaign.Name,
                    UpdatedAt = entry.Campaign.UpdatedAt,
                    NodeCount = nodes.Count,
                    EdgeCount = edges.Count,
                    CoverImageUrl = entry.Campaign.CoverImageUrl,
                    AccessRole = NormalizeAccessRole(entry.Role),
                    IsOwner = false
                };
            }).ToList();

            var combined = ownedList
                .Concat(sharedList)
                .OrderByDescending(c => c.UpdatedAt)
                .ToList();

            return Ok(combined);
        }

        // GET /api/mapforge/campaigns/{id}  (csak saját)
        [HttpGet("campaigns/{id}")]
        public async Task<ActionResult<Campaign>> GetCampaign(string id)
        {
            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var accessRole = await GetAccessRoleAsync(entity, userId);
            if (accessRole is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            return Ok(new Campaign
            {
                Id = entity.Id,
                Name = entity.Name,
                Nodes = FromJson(entity.NodesJson ?? "[]", new List<FlowNode>()),
                Edges = FromJson(entity.EdgesJson ?? "[]", new List<FlowEdge>()),
                UpdatedAt = entity.UpdatedAt,
                CoverImageUrl = entity.CoverImageUrl,
                AccessRole = accessRole,
                IsOwner = accessRole == AccessOwner
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
                .FirstOrDefaultAsync(c => c.Id == id);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var accessRole = await GetAccessRoleAsync(entity, userId);
            if (accessRole is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            if (!CanEdit(accessRole))
                return StatusCode(403, new ApiError("forbidden", "You don't have permission to edit this campaign."));

            if (accessRole == AccessEditor)
            {
                if (req.Nodes is null)
                    return StatusCode(403, new ApiError("node_delete_forbidden", "Editors cannot delete nodes."));

                var existingNodes = FromJson(entity.NodesJson ?? "[]", new List<FlowNode>());
                var incomingNodes = req.Nodes ?? new List<FlowNode>();
                if (HasDeletedNodes(existingNodes, incomingNodes))
                    return StatusCode(403, new ApiError("node_delete_forbidden", "Editors cannot delete nodes."));
            }

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
                CoverImageUrl = entity.CoverImageUrl,
                AccessRole = accessRole,
                IsOwner = accessRole == AccessOwner
            });
        }

        // PATCH /api/mapforge/campaigns/{id}/name
        [HttpPatch("campaigns/{id}/name")]
        public async Task<ActionResult<Campaign>> RenameCampaign(string id, [FromBody] RenameCampaignRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var accessRole = await GetAccessRoleAsync(entity, userId);
            if (accessRole is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            if (!CanEdit(accessRole))
                return StatusCode(403, new ApiError("forbidden", "You don't have permission to edit this campaign."));

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
                CoverImageUrl = entity.CoverImageUrl,
                AccessRole = accessRole,
                IsOwner = accessRole == AccessOwner
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

            var shares = _db.MapCampaignShares.Where(s => s.CampaignId == id);
            _db.MapCampaignShares.RemoveRange(shares);
            var invites = _db.MapCampaignInvites.Where(i => i.CampaignId == id);
            _db.MapCampaignInvites.RemoveRange(invites);
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
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxImageBytes)]
        public async Task<ActionResult<object>> UploadCampaignCover(string id, IFormFile file)
        {
            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var accessRole = await GetAccessRoleAsync(entity, userId);
            if (accessRole is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            if (!CanEdit(accessRole))
                return StatusCode(403, new ApiError("forbidden", "You don't have permission to edit this campaign."));

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
                .FirstOrDefaultAsync(c => c.Id == id);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var accessRole = await GetAccessRoleAsync(entity, userId);
            if (accessRole is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            if (!CanEdit(accessRole))
                return StatusCode(403, new ApiError("forbidden", "You don't have permission to edit this campaign."));

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

            // Ha a client nem k??ldte, legyen biztosan default
            node.Data ??= new NodeData();
            node.Data.ImageUrl ??= null;
            // Statblock maradhat null (Monster eset??n majd UI k??ldi)

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
                .FirstOrDefaultAsync(c => c.Id == id);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var accessRole = await GetAccessRoleAsync(entity, userId);
            if (accessRole is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            if (!CanEdit(accessRole))
                return StatusCode(403, new ApiError("forbidden", "You don't have permission to edit this campaign."));

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

                // ??J mez??k
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
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxImageBytes)]
        public async Task<ActionResult<object>> UploadNodeImage(string id, string nodeId, IFormFile file)
        {
            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var accessRole = await GetAccessRoleAsync(entity, userId);
            if (accessRole is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            if (!CanEdit(accessRole))
                return StatusCode(403, new ApiError("forbidden", "You don't have permission to edit this campaign."));

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
                .FirstOrDefaultAsync(c => c.Id == id);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var accessRole = await GetAccessRoleAsync(entity, userId);
            if (accessRole is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            if (!CanEdit(accessRole))
                return StatusCode(403, new ApiError("forbidden", "You don't have permission to edit this campaign."));

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
                .FirstOrDefaultAsync(c => c.Id == id);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var accessRole = await GetAccessRoleAsync(entity, userId);
            if (accessRole is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            if (!CanEdit(accessRole))
                return StatusCode(403, new ApiError("forbidden", "You don't have permission to edit this campaign."));

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
                .FirstOrDefaultAsync(c => c.Id == id);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var accessRole = await GetAccessRoleAsync(entity, userId);
            if (accessRole is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            if (!CanEdit(accessRole))
                return StatusCode(403, new ApiError("forbidden", "You don't have permission to edit this campaign."));

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
        // Sharing endpoints (owner only)
        // =========================================================

        [HttpGet("campaigns/{id}/shares")]
        public async Task<ActionResult<List<CampaignShareDto>>> GetShares(string id)
        {
            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var shares = await _db.MapCampaignShares
                .AsNoTracking()
                .Where(s => s.CampaignId == id)
                .Join(_db.Users.AsNoTracking(), s => s.UserId, u => u.Id,
                    (s, u) => new CampaignShareDto
                    {
                        UserId = u.Id,
                        Username = u.Username,
                        Role = NormalizeAccessRole(s.Role)
                    })
                .OrderBy(s => s.Username)
                .ToListAsync();

            return Ok(shares);
        }

        [HttpPost("campaigns/{id}/shares")]
        public async Task<ActionResult<CampaignShareDto>> CreateShare(string id, [FromBody] CampaignShareRequest req)
        {
            return BadRequest(new ApiError("share_flow_changed", "Sharing now requires invites. Use /invites/friend or /invites/link."));
        }

        [HttpPut("campaigns/{id}/shares/{shareUserId}")]
        public async Task<ActionResult<CampaignShareDto>> UpdateShare(string id, int shareUserId, [FromBody] CampaignShareRoleRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var share = await _db.MapCampaignShares
                .FirstOrDefaultAsync(s => s.CampaignId == id && s.UserId == shareUserId);

            if (share is null)
                return NotFound(new ApiError("share_not_found", "Share not found."));

            share.Role = NormalizeAccessRole(req.Role);
            await _db.SaveChangesAsync();

            var username = await _db.Users
                .Where(u => u.Id == shareUserId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? "User";

            return Ok(new CampaignShareDto
            {
                UserId = shareUserId,
                Username = username,
                Role = NormalizeAccessRole(share.Role)
            });
        }

        [HttpDelete("campaigns/{id}/shares/{shareUserId}")]
        public async Task<IActionResult> DeleteShare(string id, int shareUserId)
        {
            var userId = GetCurrentUserId();

            var entity = await _db.MapCampaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (entity is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var share = await _db.MapCampaignShares
                .FirstOrDefaultAsync(s => s.CampaignId == id && s.UserId == shareUserId);

            if (share is null)
                return NotFound(new ApiError("share_not_found", "Share not found."));

            _db.MapCampaignShares.Remove(share);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // =========================================================
        // Invite endpoints
        // =========================================================

        [HttpPost("campaigns/{id}/invites/friend")]
        public async Task<ActionResult<CampaignInviteDto>> CreateFriendInvite(string id, [FromBody] CampaignInviteRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();

            var campaign = await _db.MapCampaigns
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (campaign is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var username = req.Username.Trim();
            var role = NormalizeAccessRole(req.Role);

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
            if (user is null)
                return NotFound(new ApiError("user_not_found", "User not found."));

            if (user.Id == userId)
                return BadRequest(new ApiError("invalid_invite", "You already own this campaign."));

            var isFriend = await IsFriendAsync(userId, user.Id);
            if (!isFriend)
                return BadRequest(new ApiError("not_friends", "You can only invite friends directly."));

            var existingShare = await _db.MapCampaignShares
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CampaignId == id && s.UserId == user.Id);
            if (existingShare is not null)
                return BadRequest(new ApiError("already_shared", "User already has access."));

            var invite = await _db.MapCampaignInvites
                .FirstOrDefaultAsync(i => i.CampaignId == id && i.TargetUserId == user.Id && i.Status == InviteStatusPending);

            if (invite is null)
            {
                invite = new MapCampaignInvite
                {
                    CampaignId = id,
                    CreatedByUserId = userId,
                    TargetUserId = user.Id,
                    Role = role,
                    Token = null,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    ExpiresAt = null,
                    AcceptedAt = null,
                    AcceptedByUserId = null,
                    Status = InviteStatusPending,
                    IsLink = false
                };
                _db.MapCampaignInvites.Add(invite);
            }
            else
            {
                invite.Role = role;
                invite.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                invite.Status = InviteStatusPending;
            }

            await _db.SaveChangesAsync();

            return Ok(new CampaignInviteDto
            {
                Id = invite.Id,
                CampaignId = invite.CampaignId,
                CampaignName = campaign.Name,
                Role = invite.Role,
                CreatedAt = invite.CreatedAt,
                ExpiresAt = invite.ExpiresAt,
                Status = invite.Status,
                IsLink = invite.IsLink
            });
        }

        [HttpPost("campaigns/{id}/invites/link")]
        public async Task<ActionResult<CampaignInviteLinkDto>> CreateLinkInvite(string id, [FromBody] CampaignInviteLinkRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();

            var campaign = await _db.MapCampaigns
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);

            if (campaign is null)
                return NotFound(new ApiError("campaign_not_found", "Campaign not found."));

            var role = NormalizeAccessRole(req.Role);
            var token = GenerateInviteToken();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var expiresAt = now + (10 * 60 * 1000);

            var invite = new MapCampaignInvite
            {
                CampaignId = id,
                CreatedByUserId = userId,
                TargetUserId = null,
                Role = role,
                Token = token,
                CreatedAt = now,
                ExpiresAt = expiresAt,
                AcceptedAt = null,
                AcceptedByUserId = null,
                Status = InviteStatusPending,
                IsLink = true
            };

            _db.MapCampaignInvites.Add(invite);
            await _db.SaveChangesAsync();

            return Ok(new CampaignInviteLinkDto
            {
                Token = token,
                Role = role,
                ExpiresAt = expiresAt
            });
        }

        [HttpGet("invites")]
        public async Task<ActionResult<List<CampaignInviteDto>>> GetMyInvites()
        {
            var userId = GetCurrentUserId();

            var invites = await _db.MapCampaignInvites
                .AsNoTracking()
                .Where(i => i.TargetUserId == userId && i.Status == InviteStatusPending)
                .Join(_db.MapCampaigns.AsNoTracking(), i => i.CampaignId, c => c.Id, (i, c) => new { Invite = i, Campaign = c })
                .ToListAsync();

            var list = invites.Select(item => new CampaignInviteDto
            {
                Id = item.Invite.Id,
                CampaignId = item.Invite.CampaignId,
                CampaignName = item.Campaign.Name,
                Role = item.Invite.Role,
                CreatedAt = item.Invite.CreatedAt,
                ExpiresAt = item.Invite.ExpiresAt,
                Status = item.Invite.Status,
                IsLink = item.Invite.IsLink
            }).ToList();

            return Ok(list);
        }

        [HttpPost("invites/{inviteId}/accept")]
        public async Task<ActionResult<CampaignShareDto>> AcceptInvite(int inviteId)
        {
            var userId = GetCurrentUserId();

            var invite = await _db.MapCampaignInvites
                .FirstOrDefaultAsync(i => i.Id == inviteId && i.TargetUserId == userId);

            if (invite is null)
                return NotFound(new ApiError("invite_not_found", "Invite not found."));

            if (invite.Status != InviteStatusPending)
                return BadRequest(new ApiError("invite_not_pending", "Invite is not pending."));

            if (IsInviteExpired(invite.ExpiresAt))
            {
                invite.Status = InviteStatusExpired;
                await _db.SaveChangesAsync();
                return BadRequest(new ApiError("invite_expired", "Invite expired."));
            }

            var existingShare = await _db.MapCampaignShares
                .FirstOrDefaultAsync(s => s.CampaignId == invite.CampaignId && s.UserId == userId);

            if (existingShare is null)
            {
                existingShare = new MapCampaignShare
                {
                    CampaignId = invite.CampaignId,
                    UserId = userId,
                    Role = NormalizeAccessRole(invite.Role),
                    SharedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    SharedByUserId = invite.CreatedByUserId
                };
                _db.MapCampaignShares.Add(existingShare);
            }

            invite.Status = InviteStatusAccepted;
            invite.AcceptedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            invite.AcceptedByUserId = userId;

            await _db.SaveChangesAsync();

            var username = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? "User";

            return Ok(new CampaignShareDto
            {
                UserId = userId,
                Username = username,
                Role = NormalizeAccessRole(existingShare.Role)
            });
        }

        [HttpPost("invites/{inviteId}/decline")]
        public async Task<IActionResult> DeclineInvite(int inviteId)
        {
            var userId = GetCurrentUserId();

            var invite = await _db.MapCampaignInvites
                .FirstOrDefaultAsync(i => i.Id == inviteId && i.TargetUserId == userId);

            if (invite is null)
                return NotFound(new ApiError("invite_not_found", "Invite not found."));

            if (invite.Status != InviteStatusPending)
                return BadRequest(new ApiError("invite_not_pending", "Invite is not pending."));

            invite.Status = InviteStatusDeclined;
            invite.AcceptedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            invite.AcceptedByUserId = userId;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("invites/claim")]
        public async Task<ActionResult<CampaignShareDto>> ClaimInvite([FromBody] CampaignInviteClaimRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            var token = req.Token.Trim();

            var invite = await _db.MapCampaignInvites
                .FirstOrDefaultAsync(i => i.Token == token && i.IsLink);

            if (invite is null)
                return NotFound(new ApiError("invite_not_found", "Invite not found."));

            if (invite.Status != InviteStatusPending)
                return BadRequest(new ApiError("invite_not_pending", "Invite is not pending."));

            if (IsInviteExpired(invite.ExpiresAt))
            {
                invite.Status = InviteStatusExpired;
                await _db.SaveChangesAsync();
                return BadRequest(new ApiError("invite_expired", "Invite expired."));
            }

            var existingShare = await _db.MapCampaignShares
                .FirstOrDefaultAsync(s => s.CampaignId == invite.CampaignId && s.UserId == userId);

            if (existingShare is null)
            {
                existingShare = new MapCampaignShare
                {
                    CampaignId = invite.CampaignId,
                    UserId = userId,
                    Role = NormalizeAccessRole(invite.Role),
                    SharedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    SharedByUserId = invite.CreatedByUserId
                };
                _db.MapCampaignShares.Add(existingShare);
            }

            invite.Status = InviteStatusAccepted;
            invite.AcceptedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            invite.AcceptedByUserId = userId;

            await _db.SaveChangesAsync();

            var username = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? "User";

            return Ok(new CampaignShareDto
            {
                UserId = userId,
                Username = username,
                Role = NormalizeAccessRole(existingShare.Role)
            });
        }

        // =========================================================
        // Models + DB entity (same file)
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
// End of file Controllers/DMTools/MapForgeController.cs
