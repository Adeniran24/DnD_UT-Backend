using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChallengeRatingCalculator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChallengeRatingController : ControllerBase
    {
        private static readonly List<Party> _parties = new();
        private readonly HttpClient _httpClient;

        public ChallengeRatingController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        // GET: api/ChallengeRating/monsters
        [HttpGet("monsters")]
        public async Task<ActionResult<List<MonsterDto>>> GetMonsters()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:5188/api/Monsters");
                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Failed to fetch monsters");

                var json = await response.Content.ReadAsStringAsync();
                var monsters = JsonSerializer.Deserialize<List<MonsterDto>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                return Ok(monsters);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching monsters: {ex.Message}");
            }
        }

        // POST: api/ChallengeRating/party
        [HttpPost("party")]
        public ActionResult<Party> CreateParty()
        {
            var party = new Party();
            _parties.Add(party);
            return Ok(party);
        }

        // POST: api/ChallengeRating/party/{partyId}/character
        [HttpPost("party/{partyId}/character")]
        public ActionResult<Party> AddCharacter(string partyId, [FromBody] Character character)
        {
            var party = _parties.FirstOrDefault(p => p.Id == partyId);
            if (party == null) return NotFound();

            party.Characters.Add(character);
            return Ok(party);
        }

        // POST: api/ChallengeRating/party/{partyId}/monster
        [HttpPost("party/{partyId}/monster")]
        public ActionResult<Party> AddMonster(string partyId, [FromBody] MonsterDto monster)
        {
            var party = _parties.FirstOrDefault(p => p.Id == partyId);
            if (party == null) return NotFound();

            party.Monsters.Add(monster);
            return Ok(party);
        }

        // DELETE: api/ChallengeRating/party/{partyId}/character/{index}
        [HttpDelete("party/{partyId}/character/{index}")]
        public ActionResult<Party> RemoveCharacter(string partyId, int index)
        {
            var party = _parties.FirstOrDefault(p => p.Id == partyId);
            if (party == null) return NotFound();
            if (index < 0 || index >= party.Characters.Count) return BadRequest();

            party.Characters.RemoveAt(index);
            return Ok(party);
        }

        // DELETE: api/ChallengeRating/party/{partyId}/monster/{index}
        [HttpDelete("party/{partyId}/monster/{index}")]
        public ActionResult<Party> RemoveMonster(string partyId, int index)
        {
            var party = _parties.FirstOrDefault(p => p.Id == partyId);
            if (party == null) return NotFound();
            if (index < 0 || index >= party.Monsters.Count) return BadRequest();

            party.Monsters.RemoveAt(index);
            return Ok(party);
        }

        // GET: api/ChallengeRating/party/{partyId}
        [HttpGet("party/{partyId}")]
        public ActionResult<Party> GetParty(string partyId)
        {
            var party = _parties.FirstOrDefault(p => p.Id == partyId);
            if (party == null) return NotFound();
            return Ok(party);
        }
    }

    // DTO for monsters returned by your API
    public class MonsterDto
    {
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Desc { get; set; }
        public string Size { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int HitPoints { get; set; }
        public string HitDice { get; set; } = string.Empty;
        public string ChallengeRating { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
    }

    public class Character
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
    }

    public class Party
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public List<Character> Characters { get; set; } = new List<Character>();
        public List<MonsterDto> Monsters { get; set; } = new List<MonsterDto>();
    }
}
