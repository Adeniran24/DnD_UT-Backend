using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GameApi.Data;
using GameApi.Hubs;
using GameApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/vtt")]
    [Authorize]
    public class VttController : ControllerBase
    {
        private const long MaxUploadBytes = 10 * 1024 * 1024;
        private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<VttHub> _hub;

        public VttController(AppDbContext context, IWebHostEnvironment env, IHubContext<VttHub> hub)
        {
            _context = context;
            _env = env;
            _hub = hub;
        }

        private int Me
        {
            get
            {
                var idStr =
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue("id") ??
                    User.FindFirstValue("userId") ??
                    User.FindFirstValue("sub");

                if (!int.TryParse(idStr, out var userId))
                {
                    throw new UnauthorizedAccessException("Missing/invalid user id claim in JWT.");
                }

                return userId;
            }
        }

        private async Task<(VttSession? session, VttRole? role)> TryGetSessionRole(int sessionId)
        {
            var session = await _context.VttSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null)
            {
                return (null, null);
            }

            if (session.OwnerUserId == Me)
            {
                return (session, VttRole.DM);
            }

            var member = await _context.VttSessionMembers
                .FirstOrDefaultAsync(m => m.SessionId == sessionId && m.UserId == Me);

            if (member == null)
            {
                return (session, null);
            }

            return (session, member.Role);
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var userId = Me;

            var sessions = await _context.VttSessions
                .Where(s => s.OwnerUserId == userId || s.Members.Any(m => m.UserId == userId))
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    ownerUserId = s.OwnerUserId,
                    ownerName = s.Owner.Username,
                    role = s.OwnerUserId == userId
                        ? VttRole.DM
                        : s.Members.Where(m => m.UserId == userId).Select(m => m.Role).FirstOrDefault(),
                    mapImageUrl = s.Maps.Select(m => m.ImageUrl).FirstOrDefault(),
                    updatedAt = s.UpdatedAt
                })
                .OrderByDescending(s => s.updatedAt)
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var session = new VttSession
            {
                Name = request.Name.Trim(),
                OwnerUserId = Me,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VttSessions.Add(session);
            await _context.SaveChangesAsync();

            var member = new VttSessionMember
            {
                SessionId = session.Id,
                UserId = Me,
                Role = VttRole.DM,
                JoinedAt = DateTime.UtcNow
            };

            var map = new VttMap
            {
                SessionId = session.Id,
                Name = "Main Map",
                GridSize = 50,
                Width = 50,
                Height = 50,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VttSessionMembers.Add(member);
            _context.VttMaps.Add(map);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                session = new
                {
                    id = session.Id,
                    name = session.Name,
                    ownerUserId = session.OwnerUserId
                },
                role = VttRole.DM.ToString(),
                map = new
                {
                    id = map.Id,
                    name = map.Name,
                    imageUrl = map.ImageUrl,
                    gridSize = map.GridSize,
                    gridOffsetX = map.GridOffsetX,
                    gridOffsetY = map.GridOffsetY,
                    width = map.Width,
                    height = map.Height
                }
            });
        }

        [HttpPost("sessions/{id}/join")]
        public async Task<IActionResult> JoinSession(int id)
        {
            var session = await _context.VttSessions.FirstOrDefaultAsync(s => s.Id == id);
            if (session == null) return NotFound();

            var userId = Me;

            var member = await _context.VttSessionMembers
                .FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == userId);

            if (member == null)
            {
                member = new VttSessionMember
                {
                    SessionId = id,
                    UserId = userId,
                    Role = VttRole.Player,
                    JoinedAt = DateTime.UtcNow
                };

                _context.VttSessionMembers.Add(member);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                sessionId = id,
                role = member.Role.ToString()
            });
        }

        [HttpGet("sessions/{id}/state")]
        public async Task<IActionResult> GetState(int id)
        {
            var (session, role) = await TryGetSessionRole(id);
            if (session == null) return NotFound();
            if (role == null) return Forbid();

            var map = await _context.VttMaps
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.SessionId == id);

            var isDm = role == VttRole.DM;

            var tokensQuery = _context.VttTokens
                .AsNoTracking()
                .Where(t => t.SessionId == id);

            if (!isDm)
            {
                tokensQuery = tokensQuery.Where(t => !t.IsHidden || t.OwnerUserId == Me);
            }

            var tokens = await tokensQuery
                .Select(t => new
                {
                    id = t.Id,
                    sessionId = t.SessionId,
                    ownerUserId = t.OwnerUserId,
                    characterId = t.CharacterId,
                    name = t.Name,
                    imageUrl = t.ImageUrl,
                    x = t.X,
                    y = t.Y,
                    width = t.Width,
                    height = t.Height,
                    rotation = t.Rotation,
                    isHidden = t.IsHidden,
                    isLocked = t.IsLocked,
                    updatedAt = t.UpdatedAt
                })
                .ToListAsync();

            var members = await _context.VttSessionMembers
                .Where(m => m.SessionId == id)
                .Select(m => new
                {
                    userId = m.UserId,
                    username = m.User.Username,
                    role = m.Role.ToString()
                })
                .ToListAsync();

            var chat = await _context.VttChatMessages
                .Where(m => m.SessionId == id)
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    id = m.Id,
                    userId = m.UserId,
                    username = m.User.Username,
                    type = m.Type,
                    content = m.Content,
                    payloadJson = m.PayloadJson,
                    createdAt = m.CreatedAt
                })
                .ToListAsync();

            var initiativeEntries = await _context.VttInitiativeEntries
                .AsNoTracking()
                .Where(i => i.SessionId == id)
                .OrderByDescending(i => i.Value)
                .ThenBy(i => i.CreatedAt)
                .ThenBy(i => i.Id)
                .Select(i => new
                {
                    id = i.Id,
                    sessionId = i.SessionId,
                    tokenId = i.TokenId,
                    name = i.Name,
                    value = i.Value,
                    createdAt = i.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                session = new
                {
                    id = session.Id,
                    name = session.Name,
                    ownerUserId = session.OwnerUserId
                },
                role = role.Value.ToString(),
                map = map == null
                    ? null
                    : new
                    {
                        id = map.Id,
                        name = map.Name,
                        imageUrl = map.ImageUrl,
                        gridSize = map.GridSize,
                        gridOffsetX = map.GridOffsetX,
                        gridOffsetY = map.GridOffsetY,
                        width = map.Width,
                        height = map.Height,
                        updatedAt = map.UpdatedAt
                    },
                tokens,
                members,
                chat,
                initiative = new
                {
                    entries = initiativeEntries,
                    activeEntryId = session.InitiativeActiveEntryId,
                    round = session.InitiativeRound < 1 ? 1 : session.InitiativeRound
                }
            });
        }

        [HttpPut("sessions/{id}/map")]
        public async Task<IActionResult> UpdateMap(int id, [FromBody] UpdateMapRequest request)
        {
            var (session, role) = await TryGetSessionRole(id);
            if (session == null) return NotFound();
            if (role != VttRole.DM) return Forbid();

            var map = await _context.VttMaps.FirstOrDefaultAsync(m => m.SessionId == id);
            if (map == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Name)) map.Name = request.Name.Trim();
            if (request.ImageUrl != null) map.ImageUrl = request.ImageUrl;
            if (request.GridSize.HasValue && request.GridSize.Value > 0) map.GridSize = request.GridSize.Value;
            if (request.GridOffsetX.HasValue) map.GridOffsetX = request.GridOffsetX.Value;
            if (request.GridOffsetY.HasValue) map.GridOffsetY = request.GridOffsetY.Value;
            if (request.Width.HasValue && request.Width.Value > 0) map.Width = request.Width.Value;
            if (request.Height.HasValue && request.Height.Value > 0) map.Height = request.Height.Value;

            map.UpdatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"vtt:{id}").SendAsync("mapUpdated", new
            {
                id = map.Id,
                sessionId = id,
                name = map.Name,
                imageUrl = map.ImageUrl,
                gridSize = map.GridSize,
                gridOffsetX = map.GridOffsetX,
                gridOffsetY = map.GridOffsetY,
                width = map.Width,
                height = map.Height,
                updatedAt = map.UpdatedAt
            });

            return Ok(new
            {
                id = map.Id,
                name = map.Name,
                imageUrl = map.ImageUrl,
                gridSize = map.GridSize,
                gridOffsetX = map.GridOffsetX,
                gridOffsetY = map.GridOffsetY,
                width = map.Width,
                height = map.Height,
                updatedAt = map.UpdatedAt
            });
        }

        [HttpPost("sessions/{id}/map/image")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxUploadBytes)]
        public async Task<IActionResult> UploadMapImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            var (session, role) = await TryGetSessionRole(id);
            if (session == null) return NotFound();
            if (role != VttRole.DM) return Forbid();

            if (!AllowedImageTypes.Contains(file.ContentType))
            {
                return BadRequest("Invalid image type.");
            }

            var map = await _context.VttMaps.FirstOrDefaultAsync(m => m.SessionId == id);
            if (map == null) return NotFound();

            var asset = await SaveAssetAsync(id, file, "map");
            map.ImageUrl = asset.Url;
            map.UpdatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"vtt:{id}").SendAsync("mapUpdated", new
            {
                id = map.Id,
                sessionId = id,
                name = map.Name,
                imageUrl = map.ImageUrl,
                gridSize = map.GridSize,
                gridOffsetX = map.GridOffsetX,
                gridOffsetY = map.GridOffsetY,
                width = map.Width,
                height = map.Height,
                updatedAt = map.UpdatedAt
            });

            return Ok(new { imageUrl = map.ImageUrl, assetId = asset.Id });
        }

        [HttpPost("sessions/{id}/assets")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxUploadBytes)]
        public async Task<IActionResult> UploadAsset(int id, IFormFile file, [FromForm] string? kind)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            var (session, role) = await TryGetSessionRole(id);
            if (session == null) return NotFound();
            if (role == null) return Forbid();

            if (!AllowedImageTypes.Contains(file.ContentType))
            {
                return BadRequest("Invalid image type.");
            }

            var asset = await SaveAssetAsync(id, file, string.IsNullOrWhiteSpace(kind) ? "misc" : kind.Trim());

            return Ok(new
            {
                id = asset.Id,
                kind = asset.Kind,
                url = asset.Url,
                originalFileName = asset.OriginalFileName,
                contentType = asset.ContentType,
                sizeBytes = asset.SizeBytes,
                createdAt = asset.CreatedAt
            });
        }

        private async Task<VttAsset> SaveAssetAsync(int sessionId, IFormFile file, string kind)
        {
            if (file.Length > MaxUploadBytes)
            {
                throw new ValidationException("File too large.");
            }

            var safeKind = kind.ToLowerInvariant();
            if (safeKind.Length > 24)
            {
                safeKind = safeKind[..24];
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = file.ContentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    _ => ".img"
                };
            }

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var baseFolder = Path.Combine(webRoot, "uploads", "vtt", "sessions", sessionId.ToString(), safeKind);
            Directory.CreateDirectory(baseFolder);

            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(baseFolder, storedFileName);

            await using (var stream = System.IO.File.Create(physicalPath))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/uploads/vtt/sessions/{sessionId}/{safeKind}/{storedFileName}";

            var asset = new VttAsset
            {
                SessionId = sessionId,
                UploadedByUserId = Me,
                Kind = safeKind,
                OriginalFileName = file.FileName,
                StoredFileName = storedFileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                Url = url,
                CreatedAt = DateTime.UtcNow
            };

            _context.VttAssets.Add(asset);
            await _context.SaveChangesAsync();

            return asset;
        }

        public sealed class CreateSessionRequest
        {
            [Required, MinLength(1), MaxLength(80)]
            public string Name { get; set; } = string.Empty;
        }

        public sealed class UpdateMapRequest
        {
            public string? Name { get; set; }
            public string? ImageUrl { get; set; }
            public int? GridSize { get; set; }
            public int? GridOffsetX { get; set; }
            public int? GridOffsetY { get; set; }
            public int? Width { get; set; }
            public int? Height { get; set; }
        }
    }
}
