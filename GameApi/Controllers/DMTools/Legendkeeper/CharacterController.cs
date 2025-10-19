using Microsoft.AspNetCore.Mvc;
using LegendKeeper.Models.DMTools.LegendKeeper;

namespace LegendKeeper.Controllers.DMTools.LegendKeeper
{
    [ApiController]
    [Route("api/dmtools/legendkeeper/[controller]")]
    public class CharactersController : ControllerBase
    {
        private static List<Character> characters = new List<Character>();

        [HttpGet]
        public IActionResult GetAll() => Ok(characters);

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var character = characters.FirstOrDefault(c => c.Id == id);
            return character == null ? NotFound() : Ok(character);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Character character)
        {
            character.Id = characters.Count + 1;
            characters.Add(character);
            return CreatedAtAction(nameof(Get), new { id = character.Id }, character);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Character updated)
        {
            var character = characters.FirstOrDefault(c => c.Id == id);
            if (character == null) return NotFound();

            character.Name = updated.Name;
            character.Race = updated.Race;
            character.Class = updated.Class;
            character.Description = updated.Description;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var character = characters.FirstOrDefault(c => c.Id == id);
            if (character == null) return NotFound();

            characters.Remove(character);
            return NoContent();
        }
    }
}
