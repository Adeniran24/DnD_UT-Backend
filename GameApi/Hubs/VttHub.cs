using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameApi.Data;
using GameApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Hubs
{
    [Authorize]
    public class VttHub : Hub
    {
        private readonly AppDbContext _context;

        public VttHub(AppDbContext context)
        {
            _context = context;
        }

        private int Me
        {
            get
            {
                var idStr =
                    Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    Context.User?.FindFirstValue("id") ??
                    Context.User?.FindFirstValue("userId") ??
                    Context.User?.FindFirstValue("sub");

                if (!int.TryParse(idStr, out var userId))
                {
                    throw new HubException("Missing/invalid user id claim in JWT.");
                }

                return userId;
            }
        }

        private async Task<(VttSession session, VttRole role)> GetSessionRole(int sessionId)
        {
            var session = await _context.VttSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null)
            {
                throw new HubException("Session not found.");
            }

            if (session.OwnerUserId == Me)
            {
                return (session, VttRole.DM);
            }

            var membership = await _context.VttSessionMembers
                .FirstOrDefaultAsync(m => m.SessionId == sessionId && m.UserId == Me);

            if (membership == null)
            {
                throw new HubException("Not a member.");
            }

            return (session, membership.Role);
        }

        private async Task<List<VttInitiativeEntry>> LoadInitiativeEntries(int sessionId)
        {
            return await _context.VttInitiativeEntries
                .AsNoTracking()
                .Where(i => i.SessionId == sessionId)
                .OrderByDescending(i => i.Value)
                .ThenBy(i => i.CreatedAt)
                .ThenBy(i => i.Id)
                .ToListAsync();
        }

        private async Task BroadcastInitiative(int sessionId, VttSession? session = null)
        {
            session ??= await _context.VttSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                return;
            }

            var entries = await LoadInitiativeEntries(sessionId);

            await Clients.Group($"vtt:{sessionId}").SendAsync("initiativeUpdated", new
            {
                entries = entries.Select(i => new
                {
                    id = i.Id,
                    sessionId = i.SessionId,
                    tokenId = i.TokenId,
                    name = i.Name,
                    value = i.Value,
                    createdAt = i.CreatedAt
                }),
                activeEntryId = session.InitiativeActiveEntryId,
                round = session.InitiativeRound < 1 ? 1 : session.InitiativeRound
            });
        }

        public async Task JoinSession(int sessionId)
        {
            await GetSessionRole(sessionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"vtt:{sessionId}");
        }

        public async Task LeaveSession(int sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"vtt:{sessionId}");
        }

        public async Task SendChat(int sessionId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new HubException("Empty message.");
            }

            await GetSessionRole(sessionId);

            var message = new VttChatMessage
            {
                SessionId = sessionId,
                UserId = Me,
                Type = "chat",
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.VttChatMessages.Add(message);
            await _context.SaveChangesAsync();

            var username = await _context.Users
                .Where(u => u.Id == Me)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? "User";

            await Clients.Group($"vtt:{sessionId}").SendAsync("chatReceived", new
            {
                id = message.Id,
                sessionId,
                userId = Me,
                username,
                type = message.Type,
                content = message.Content,
                payloadJson = message.PayloadJson,
                createdAt = message.CreatedAt
            });
        }

        public async Task RollDice(int sessionId, string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new HubException("Empty roll.");
            }

            await GetSessionRole(sessionId);

            var result = DiceRoller.Roll(expression);
            var payloadJson = JsonSerializer.Serialize(result);

            var message = new VttChatMessage
            {
                SessionId = sessionId,
                UserId = Me,
                Type = "roll",
                Content = $"{result.Expression} = {result.Total}",
                PayloadJson = payloadJson,
                CreatedAt = DateTime.UtcNow
            };

            _context.VttChatMessages.Add(message);
            await _context.SaveChangesAsync();

            var username = await _context.Users
                .Where(u => u.Id == Me)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? "User";

            await Clients.Group($"vtt:{sessionId}").SendAsync("chatReceived", new
            {
                id = message.Id,
                sessionId,
                userId = Me,
                username,
                type = message.Type,
                content = message.Content,
                payloadJson = message.PayloadJson,
                createdAt = message.CreatedAt
            });
        }

        public async Task SendPing(int sessionId, PingRequest request)
        {
            await GetSessionRole(sessionId);

            if (request == null)
            {
                throw new HubException("Invalid ping.");
            }

            var username = await _context.Users
                .Where(u => u.Id == Me)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? "User";

            await Clients.Group($"vtt:{sessionId}").SendAsync("ping", new
            {
                sessionId,
                userId = Me,
                username,
                x = request.X,
                y = request.Y
            });
        }

        public async Task CreateToken(int sessionId, CreateTokenRequest request)
        {
            var (session, role) = await GetSessionRole(sessionId);
            var isDm = role == VttRole.DM;

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new HubException("Token name is required.");
            }

            if (!isDm)
            {
                request.OwnerUserId = Me;
            }
            else if (request.OwnerUserId.HasValue)
            {
                var isMember = await _context.VttSessionMembers
                    .AnyAsync(m => m.SessionId == sessionId && m.UserId == request.OwnerUserId.Value);

                if (!isMember && request.OwnerUserId.Value != session.OwnerUserId)
                {
                    throw new HubException("Token owner must be a session member.");
                }
            }

            if (request.CharacterId.HasValue)
            {
                var character = await _context.Characters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.id == request.CharacterId.Value);

                if (character == null)
                {
                    throw new HubException("Character not found.");
                }

                if (!isDm && character.userId != Me)
                {
                    throw new HubException("Not allowed.");
                }

                if (character.userId.HasValue)
                {
                    request.OwnerUserId ??= character.userId.Value;
                }
            }

            var token = new VttToken
            {
                SessionId = sessionId,
                OwnerUserId = request.OwnerUserId,
                CharacterId = request.CharacterId,
                Name = request.Name.Trim(),
                ImageUrl = request.ImageUrl,
                X = request.X,
                Y = request.Y,
                Width = request.Width <= 0 ? 1 : request.Width,
                Height = request.Height <= 0 ? 1 : request.Height,
                Rotation = request.Rotation,
                IsHidden = request.IsHidden,
                IsLocked = request.IsLocked,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VttTokens.Add(token);
            await _context.SaveChangesAsync();

            await Clients.Group($"vtt:{sessionId}").SendAsync("tokenCreated", new
            {
                id = token.Id,
                sessionId,
                ownerUserId = token.OwnerUserId,
                characterId = token.CharacterId,
                name = token.Name,
                imageUrl = token.ImageUrl,
                x = token.X,
                y = token.Y,
                width = token.Width,
                height = token.Height,
                rotation = token.Rotation,
                isHidden = token.IsHidden,
                isLocked = token.IsLocked,
                updatedAt = token.UpdatedAt
            });
        }

        public async Task UpdateToken(int sessionId, UpdateTokenRequest request)
        {
            var (_, role) = await GetSessionRole(sessionId);

            var token = await _context.VttTokens
                .FirstOrDefaultAsync(t => t.Id == request.TokenId && t.SessionId == sessionId);

            if (token == null)
            {
                throw new HubException("Token not found.");
            }

            var isDm = role == VttRole.DM;

            if (!isDm && token.OwnerUserId != Me)
            {
                throw new HubException("Not allowed.");
            }

            if (!isDm && token.IsLocked)
            {
                throw new HubException("Token is locked.");
            }

            if (request.X.HasValue) token.X = request.X.Value;
            if (request.Y.HasValue) token.Y = request.Y.Value;
            if (request.Width.HasValue && request.Width.Value > 0) token.Width = request.Width.Value;
            if (request.Height.HasValue && request.Height.Value > 0) token.Height = request.Height.Value;
            if (request.Rotation.HasValue) token.Rotation = request.Rotation.Value;

            if (isDm)
            {
                if (request.Name != null) token.Name = request.Name.Trim();
                if (request.ImageUrl != null) token.ImageUrl = request.ImageUrl;
                if (request.OwnerUserId.HasValue) token.OwnerUserId = request.OwnerUserId.Value;
                if (request.CharacterId.HasValue) token.CharacterId = request.CharacterId.Value;
                if (request.IsHidden.HasValue) token.IsHidden = request.IsHidden.Value;
                if (request.IsLocked.HasValue) token.IsLocked = request.IsLocked.Value;
            }

            token.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await Clients.Group($"vtt:{sessionId}").SendAsync("tokenUpdated", new
            {
                id = token.Id,
                sessionId,
                ownerUserId = token.OwnerUserId,
                characterId = token.CharacterId,
                name = token.Name,
                imageUrl = token.ImageUrl,
                x = token.X,
                y = token.Y,
                width = token.Width,
                height = token.Height,
                rotation = token.Rotation,
                isHidden = token.IsHidden,
                isLocked = token.IsLocked,
                updatedAt = token.UpdatedAt
            });
        }

        public async Task DeleteToken(int sessionId, int tokenId)
        {
            var (_, role) = await GetSessionRole(sessionId);

            var token = await _context.VttTokens
                .FirstOrDefaultAsync(t => t.Id == tokenId && t.SessionId == sessionId);

            if (token == null)
            {
                throw new HubException("Token not found.");
            }

            var isDm = role == VttRole.DM;
            if (!isDm && token.OwnerUserId != Me)
            {
                throw new HubException("Not allowed.");
            }

            _context.VttTokens.Remove(token);
            await _context.SaveChangesAsync();

            await Clients.Group($"vtt:{sessionId}").SendAsync("tokenDeleted", new
            {
                id = tokenId,
                sessionId
            });
        }

        public async Task UpdateMap(int sessionId, UpdateMapRequest request)
        {
            var (_, role) = await GetSessionRole(sessionId);
            if (role != VttRole.DM)
            {
                throw new HubException("DM only.");
            }

            var map = await _context.VttMaps
                .FirstOrDefaultAsync(m => m.SessionId == sessionId);

            if (map == null)
            {
                throw new HubException("Map not found.");
            }

            if (request.Name != null) map.Name = request.Name.Trim();
            if (request.ImageUrl != null) map.ImageUrl = request.ImageUrl;
            if (request.GridSize.HasValue && request.GridSize.Value > 0) map.GridSize = request.GridSize.Value;
            if (request.GridOffsetX.HasValue) map.GridOffsetX = request.GridOffsetX.Value;
            if (request.GridOffsetY.HasValue) map.GridOffsetY = request.GridOffsetY.Value;
            if (request.Width.HasValue && request.Width.Value > 0) map.Width = request.Width.Value;
            if (request.Height.HasValue && request.Height.Value > 0) map.Height = request.Height.Value;

            map.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await Clients.Group($"vtt:{sessionId}").SendAsync("mapUpdated", new
            {
                id = map.Id,
                sessionId,
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

        public async Task AddInitiativeEntry(int sessionId, AddInitiativeEntryRequest request)
        {
            var (session, role) = await GetSessionRole(sessionId);
            if (role != VttRole.DM)
            {
                throw new HubException("DM only.");
            }

            if (request == null)
            {
                throw new HubException("Invalid initiative entry.");
            }

            string? name = request.Name?.Trim();

            if (request.TokenId.HasValue)
            {
                var token = await _context.VttTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == request.TokenId.Value && t.SessionId == sessionId);

                if (token == null)
                {
                    throw new HubException("Token not found.");
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = token.Name;
                }
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new HubException("Initiative name is required.");
            }

            var entry = new VttInitiativeEntry
            {
                SessionId = sessionId,
                TokenId = request.TokenId,
                Name = name,
                Value = request.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.VttInitiativeEntries.Add(entry);
            if (session.InitiativeRound < 1)
            {
                session.InitiativeRound = 1;
            }
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await BroadcastInitiative(sessionId, session);
        }

        public async Task UpdateInitiativeEntry(int sessionId, UpdateInitiativeEntryRequest request)
        {
            var (session, role) = await GetSessionRole(sessionId);
            if (role != VttRole.DM)
            {
                throw new HubException("DM only.");
            }

            if (request == null)
            {
                throw new HubException("Invalid initiative entry.");
            }

            var entry = await _context.VttInitiativeEntries
                .FirstOrDefaultAsync(i => i.Id == request.EntryId && i.SessionId == sessionId);

            if (entry == null)
            {
                throw new HubException("Initiative entry not found.");
            }

            if (request.Name != null)
            {
                var trimmed = request.Name.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    throw new HubException("Initiative name is required.");
                }

                entry.Name = trimmed;
            }

            if (request.Value.HasValue)
            {
                entry.Value = request.Value.Value;
            }

            if (session.InitiativeRound < 1)
            {
                session.InitiativeRound = 1;
            }
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await BroadcastInitiative(sessionId, session);
        }

        public async Task RemoveInitiativeEntry(int sessionId, int entryId)
        {
            var (session, role) = await GetSessionRole(sessionId);
            if (role != VttRole.DM)
            {
                throw new HubException("DM only.");
            }

            var entry = await _context.VttInitiativeEntries
                .FirstOrDefaultAsync(i => i.Id == entryId && i.SessionId == sessionId);

            if (entry == null)
            {
                throw new HubException("Initiative entry not found.");
            }

            _context.VttInitiativeEntries.Remove(entry);

            if (session.InitiativeActiveEntryId == entry.Id)
            {
                session.InitiativeActiveEntryId = null;
            }

            if (session.InitiativeRound < 1)
            {
                session.InitiativeRound = 1;
            }

            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await BroadcastInitiative(sessionId, session);
        }

        public async Task ClearInitiative(int sessionId)
        {
            var (session, role) = await GetSessionRole(sessionId);
            if (role != VttRole.DM)
            {
                throw new HubException("DM only.");
            }

            var entries = await _context.VttInitiativeEntries
                .Where(i => i.SessionId == sessionId)
                .ToListAsync();

            if (entries.Count > 0)
            {
                _context.VttInitiativeEntries.RemoveRange(entries);
            }

            session.InitiativeActiveEntryId = null;
            session.InitiativeRound = 1;
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await BroadcastInitiative(sessionId, session);
        }

        public async Task ResetInitiative(int sessionId)
        {
            var (session, role) = await GetSessionRole(sessionId);
            if (role != VttRole.DM)
            {
                throw new HubException("DM only.");
            }

            session.InitiativeActiveEntryId = null;
            session.InitiativeRound = 1;
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await BroadcastInitiative(sessionId, session);
        }

        public async Task StepInitiative(int sessionId, int direction)
        {
            var (session, role) = await GetSessionRole(sessionId);
            if (role != VttRole.DM)
            {
                throw new HubException("DM only.");
            }

            var entries = await LoadInitiativeEntries(sessionId);
            if (entries.Count == 0)
            {
                session.InitiativeActiveEntryId = null;
                session.InitiativeRound = 1;
                session.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await BroadcastInitiative(sessionId, session);
                return;
            }

            var orderedIds = entries.Select(e => e.Id).ToList();
            var currentIndex = session.InitiativeActiveEntryId.HasValue
                ? orderedIds.IndexOf(session.InitiativeActiveEntryId.Value)
                : -1;

            if (currentIndex < 0)
            {
                session.InitiativeActiveEntryId = direction < 0
                    ? orderedIds[^1]
                    : orderedIds[0];

                session.InitiativeRound = session.InitiativeRound < 1 ? 1 : session.InitiativeRound;
            }
            else
            {
                var nextIndex = currentIndex + direction;
                if (nextIndex >= orderedIds.Count)
                {
                    nextIndex = 0;
                    session.InitiativeRound = Math.Max(1, session.InitiativeRound + 1);
                }
                else if (nextIndex < 0)
                {
                    nextIndex = orderedIds.Count - 1;
                    session.InitiativeRound = Math.Max(1, session.InitiativeRound - 1);
                }

                session.InitiativeActiveEntryId = orderedIds[nextIndex];
            }

            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await BroadcastInitiative(sessionId, session);
        }

        public async Task SetInitiativeActive(int sessionId, int? entryId)
        {
            var (session, role) = await GetSessionRole(sessionId);
            if (role != VttRole.DM)
            {
                throw new HubException("DM only.");
            }

            if (entryId.HasValue)
            {
                var exists = await _context.VttInitiativeEntries
                    .AnyAsync(i => i.SessionId == sessionId && i.Id == entryId.Value);

                if (!exists)
                {
                    throw new HubException("Initiative entry not found.");
                }
            }

            session.InitiativeActiveEntryId = entryId;
            session.InitiativeRound = session.InitiativeRound < 1 ? 1 : session.InitiativeRound;
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await BroadcastInitiative(sessionId, session);
        }

        public sealed class CreateTokenRequest
        {
            public string Name { get; set; } = string.Empty;
            public int? OwnerUserId { get; set; }
            public int? CharacterId { get; set; }
            public string? ImageUrl { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; } = 1;
            public double Height { get; set; } = 1;
            public double Rotation { get; set; }
            public bool IsHidden { get; set; }
            public bool IsLocked { get; set; }
        }

        public sealed class UpdateTokenRequest
        {
            public int TokenId { get; set; }
            public string? Name { get; set; }
            public int? OwnerUserId { get; set; }
            public int? CharacterId { get; set; }
            public string? ImageUrl { get; set; }
            public double? X { get; set; }
            public double? Y { get; set; }
            public double? Width { get; set; }
            public double? Height { get; set; }
            public double? Rotation { get; set; }
            public bool? IsHidden { get; set; }
            public bool? IsLocked { get; set; }
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

        public sealed class AddInitiativeEntryRequest
        {
            public string? Name { get; set; }
            public int Value { get; set; }
            public int? TokenId { get; set; }
        }

        public sealed class UpdateInitiativeEntryRequest
        {
            public int EntryId { get; set; }
            public string? Name { get; set; }
            public int? Value { get; set; }
        }

        public sealed class PingRequest
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        private sealed class DiceGroup
        {
            public int Count { get; set; }
            public int Sides { get; set; }
            public List<int> Rolls { get; set; } = new();
        }

        private sealed class DiceRollResult
        {
            public string Expression { get; set; } = string.Empty;
            public int Total { get; set; }
            public List<DiceGroup> Groups { get; set; } = new();
            public int Modifier { get; set; }
        }

        private static class DiceRoller
        {
            private const int MaxDice = 200;
            private const int MaxSides = 1000;
            private static readonly Regex TokenRegex = new(@"[+-]?\d*d\d+|[+-]?\d+", RegexOptions.Compiled);

            public static DiceRollResult Roll(string expression)
            {
                var cleaned = Regex.Replace(expression, @"\s+", string.Empty).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    throw new HubException("Invalid roll.");
                }

                var matches = TokenRegex.Matches(cleaned);
                if (matches.Count == 0)
                {
                    throw new HubException("Invalid roll.");
                }

                var result = new DiceRollResult { Expression = expression.Trim() };
                var totalDice = 0;
                var total = 0;
                var modifier = 0;

                foreach (Match match in matches)
                {
                    var token = match.Value;
                    var sign = 1;
                    if (token.StartsWith("+"))
                    {
                        token = token[1..];
                    }
                    else if (token.StartsWith("-"))
                    {
                        sign = -1;
                        token = token[1..];
                    }

                    if (token.Contains('d'))
                    {
                        var parts = token.Split('d');
                        var count = string.IsNullOrEmpty(parts[0]) ? 1 : int.Parse(parts[0]);
                        var sides = int.Parse(parts[1]);

                        if (count <= 0 || sides <= 0)
                        {
                            throw new HubException("Invalid roll.");
                        }

                        totalDice += count;
                        if (totalDice > MaxDice || sides > MaxSides)
                        {
                            throw new HubException("Roll too large.");
                        }

                        var group = new DiceGroup { Count = count, Sides = sides };
                        var groupTotal = 0;
                        for (var i = 0; i < count; i++)
                        {
                            var roll = Random.Shared.Next(1, sides + 1);
                            group.Rolls.Add(roll);
                            groupTotal += roll;
                        }

                        total += sign * groupTotal;
                        if (sign < 0)
                        {
                            group.Rolls = group.Rolls.Select(r => -r).ToList();
                        }

                        result.Groups.Add(group);
                    }
                    else
                    {
                        var value = int.Parse(token) * sign;
                        total += value;
                        modifier += value;
                    }
                }

                result.Total = total;
                result.Modifier = modifier;
                return result;
            }
        }
    }
}
