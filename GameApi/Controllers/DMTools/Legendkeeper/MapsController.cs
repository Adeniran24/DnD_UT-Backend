using Microsoft.AspNetCore.Mvc;
using LegendKeeper.Models.DMTools.LegendKeeper;

namespace LegendKeeper.Controllers.DMTools.LegendKeeper
{
    [ApiController]
    [Route("api/dmtools/legendkeeper/[controller]")]
    public class MapsController : ControllerBase
    {
        private static List<Map> maps = new List<Map>();

        [HttpGet]
        public IActionResult GetAll() => Ok(maps);

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var map = maps.FirstOrDefault(m => m.Id == id);
            return map == null ? NotFound() : Ok(map);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Map map)
        {
            map.Id = maps.Count + 1;
            maps.Add(map);
            return CreatedAtAction(nameof(Get), new { id = map.Id }, map);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Map updated)
        {
            var map = maps.FirstOrDefault(m => m.Id == id);
            if (map == null) return NotFound();

            map.Name = updated.Name;
            map.Description = updated.Description;
            map.ImageUrl = updated.ImageUrl;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var map = maps.FirstOrDefault(m => m.Id == id);
            if (map == null) return NotFound();

            maps.Remove(map);
            return NoContent();
        }
    }
}
