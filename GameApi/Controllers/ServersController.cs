using System.Security.Claims;
using GameApi.Data;
using GameApi.DTOs;
using GameApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/servers")]
    [Authorize]
    public class ServersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServersController(AppDbContext context)
        {
            _context = context;
        }

        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServerSummaryDto>>> GetServers()
        {
            var servers = await _context.Communities
                .Include(c => c.Users)
                .Where(c => c.Users.Any(u => u.UserId == Me) || !c.IsPrivate)
                .Select(c => new ServerSummaryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    OwnerId = c.OwnerId,
                    IsPrivate = c.IsPrivate,
                    CoverImage = c.CoverImage,
                    MemberCount = c.Users.Count
                })
                .ToListAsync();

            return servers;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServerDetailDto>> GetServer(int id)
        {
            var server = await _context.Communities
                .Include(c => c.Users)
                .Include(c => c.Channels)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (server == null)
            {
                return NotFound();
            }

            var isMember = server.Users.Any(u => u.UserId == Me);
            if (server.IsPrivate && !isMember)
            {
                return Forbid();
            }

            return new ServerDetailDto
            {
                Id = server.Id,
                Name = server.Name,
                Description = server.Description,
                OwnerId = server.OwnerId,
                IsPrivate = server.IsPrivate,
                CoverImage = server.CoverImage,
                MemberCount = server.Users.Count,
                Channels = server.Channels
                    .OrderBy(ch => ch.Position)
                    .Select(ch => new ChannelDto
                    {
                        Id = ch.Id,
                        CommunityId = ch.CommunityId,
                        Name = ch.Name,
                        Type = ch.Type,
                        IsPrivate = ch.IsPrivate,
                        Topic = ch.Topic,
                        ParentId = ch.ParentId,
                        Position = ch.Position,
                        IsArchived = ch.IsArchived,
                        IsReadOnly = ch.IsReadOnly
                    })
                    .ToList()
            };
        }

        [HttpGet("{id:int}/members")]
        public async Task<ActionResult<IEnumerable<ServerMemberDto>>> GetMembers(int id)
        {
            var isMember = await _context.CommunityUsers
                .AnyAsync(cu => cu.CommunityId == id && cu.UserId == Me);

            if (!isMember)
            {
                return Forbid();
            }

            var members = await _context.CommunityUsers
                .Include(cu => cu.User)
                .Where(cu => cu.CommunityId == id)
                .Select(cu => new ServerMemberDto
                {
                    UserId = cu.UserId,
                    Username = cu.User.Username,
                    Role = cu.Role.ToString(),
                    IsActive = cu.User.IsActive,
                    ProfilePictureUrl = cu.User.ProfilePictureUrl
                })
                .ToListAsync();

            return members;
        }

        [HttpPost]
        public async Task<ActionResult<ServerSummaryDto>> CreateServer(ServerCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Name is required.");
            }

            var server = new Community
            {
                Name = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                OwnerId = Me,
                IsPrivate = dto.IsPrivate,
                CoverImage = dto.CoverImage
            };

            _context.Communities.Add(server);
            await _context.SaveChangesAsync();

            _context.CommunityUsers.Add(new CommunityUser
            {
                CommunityId = server.Id,
                UserId = Me,
                Role = CommunityRole.Owner
            });

            var textCategory = new Channel
            {
                CommunityId = server.Id,
                Name = "Text Channels",
                Type = ChannelType.Category,
                Position = 0
            };

            var voiceCategory = new Channel
            {
                CommunityId = server.Id,
                Name = "Voice Channels",
                Type = ChannelType.Category,
                Position = 1
            };

            _context.Channels.AddRange(textCategory, voiceCategory);
            await _context.SaveChangesAsync();

            _context.Channels.AddRange(
                new Channel
                {
                    CommunityId = server.Id,
                    Name = "general",
                    Type = ChannelType.Text,
                    ParentId = textCategory.Id,
                    Position = 0
                },
                new Channel
                {
                    CommunityId = server.Id,
                    Name = "announcements",
                    Type = ChannelType.News,
                    ParentId = textCategory.Id,
                    Position = 1,
                    IsReadOnly = true
                },
                new Channel
                {
                    CommunityId = server.Id,
                    Name = "lobby",
                    Type = ChannelType.Voice,
                    ParentId = voiceCategory.Id,
                    Position = 0
                }
            );

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetServer), new { id = server.Id }, new ServerSummaryDto
            {
                Id = server.Id,
                Name = server.Name,
                Description = server.Description,
                OwnerId = server.OwnerId,
                IsPrivate = server.IsPrivate,
                CoverImage = server.CoverImage,
                MemberCount = 1
            });
        }

        [HttpGet("{id:int}/channels")]
        public async Task<ActionResult<IEnumerable<ChannelDto>>> GetChannels(int id)
        {
            var isMember = await _context.CommunityUsers
                .AnyAsync(cu => cu.CommunityId == id && cu.UserId == Me);

            if (!isMember)
            {
                return Forbid();
            }

            var channels = await _context.Channels
                .Where(ch => ch.CommunityId == id)
                .OrderBy(ch => ch.Position)
                .Select(ch => new ChannelDto
                {
                    Id = ch.Id,
                    CommunityId = ch.CommunityId,
                    Name = ch.Name,
                    Type = ch.Type,
                    IsPrivate = ch.IsPrivate,
                    Topic = ch.Topic,
                    ParentId = ch.ParentId,
                    Position = ch.Position,
                    IsArchived = ch.IsArchived,
                    IsReadOnly = ch.IsReadOnly
                })
                .ToListAsync();

            return channels;
        }

        [HttpPost("{id:int}/channels")]
        public async Task<ActionResult<ChannelDto>> CreateChannel(int id, CommunityChannelCreateDto dto)
        {
            var membership = await _context.CommunityUsers
                .FirstOrDefaultAsync(cu => cu.CommunityId == id && cu.UserId == Me);

            if (membership == null)
            {
                return Forbid();
            }

            if (membership.Role == CommunityRole.Member && dto.Type == ChannelType.News)
            {
                return Forbid();
            }

            var channel = new Channel
            {
                CommunityId = id,
                Name = dto.Name.Trim(),
                Type = dto.Type,
                IsPrivate = dto.IsPrivate,
                Topic = dto.Topic,
                ParentId = dto.ParentId,
                Position = dto.Position,
                IsReadOnly = dto.IsReadOnly
            };

            _context.Channels.Add(channel);
            await _context.SaveChangesAsync();

            return new ChannelDto
            {
                Id = channel.Id,
                CommunityId = channel.CommunityId,
                Name = channel.Name,
                Type = channel.Type,
                IsPrivate = channel.IsPrivate,
                Topic = channel.Topic,
                ParentId = channel.ParentId,
                Position = channel.Position,
                IsArchived = channel.IsArchived,
                IsReadOnly = channel.IsReadOnly
            };
        }
    }
}
