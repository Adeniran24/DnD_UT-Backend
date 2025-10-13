using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using GameApi.Models;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MagicItemsController : ControllerBase
    {
        private readonly List<MagicItem> _magicItems;
        private readonly ILogger<MagicItemsController> _logger;

        public MagicItemsController(ILogger<MagicItemsController> logger)
        {
            _logger = logger;
            
            try
            {
                // Load the JSON file from the Database/2014 directory
                var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "2014", "5e-SRD-Magic-Items.json");
                
                if (!System.IO.File.Exists(jsonFilePath))
                {
                    _logger.LogWarning("Magic items JSON file not found at: {FilePath}", jsonFilePath);
                    _magicItems = new List<MagicItem>();
                    return;
                }

                var jsonString = System.IO.File.ReadAllText(jsonFilePath);
                _magicItems = JsonSerializer.Deserialize<List<MagicItem>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? new List<MagicItem>();

                _logger.LogInformation("Loaded {Count} magic items from database", _magicItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading magic items from JSON file");
                _magicItems = new List<MagicItem>();
            }
        }

        /// <summary>
        /// Get all magic items
        /// </summary>
        /// <returns>List of all magic items</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<MagicItem>> GetAllMagicItems()
        {
            return Ok(_magicItems);
        }

        /// <summary>
        /// Get a specific magic item by index
        /// </summary>
        /// <param name="index">The index of the magic item (e.g., "adamantine-armor")</param>
        /// <returns>The magic item</returns>
        [HttpGet("{index}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<MagicItem> GetMagicItem(string index)
        {
            var magicItem = _magicItems.FirstOrDefault(m => 
                m.Index?.Equals(index, StringComparison.OrdinalIgnoreCase) == true);
            
            if (magicItem == null)
            {
                return NotFound($"Magic item with index '{index}' not found");
            }
            
            return Ok(magicItem);
        }

        /// <summary>
        /// Get magic items by equipment category
        /// </summary>
        /// <param name="category">Equipment category (e.g., "armor", "weapon", "wondrous-items")</param>
        /// <returns>List of magic items in the specified category</returns>
        [HttpGet("category/{category}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<MagicItem>> GetMagicItemsByCategory(string category)
        {
            var items = _magicItems
                .Where(m => m.EquipmentCategory?.Index?.Equals(category, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            if (!items.Any())
            {
                return NotFound($"No magic items found in category '{category}'");
            }
            
            return Ok(items);
        }

        /// <summary>
        /// Get magic items by rarity
        /// </summary>
        /// <param name="rarity">Rarity level (e.g., "common", "uncommon", "rare", "very rare", "legendary")</param>
        /// <returns>List of magic items with the specified rarity</returns>
        [HttpGet("rarity/{rarity}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<MagicItem>> GetMagicItemsByRarity(string rarity)
        {
            var items = _magicItems
                .Where(m => m.Rarity?.Name?.Equals(rarity, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            if (!items.Any())
            {
                return NotFound($"No magic items found with rarity '{rarity}'");
            }
            
            return Ok(items);
        }

        /// <summary>
        /// Search magic items by name
        /// </summary>
        /// <param name="name">Name or partial name to search for</param>
        /// <returns>List of matching magic items</returns>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<IEnumerable<MagicItem>> SearchMagicItems([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name parameter is required");
            }

            var items = _magicItems
                .Where(m => m.Name?.Contains(name, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
            
            return Ok(items);
        }

        /// <summary>
        /// Get variants of a specific magic item
        /// </summary>
        /// <param name="index">The index of the magic item</param>
        /// <returns>List of variant items</returns>
        [HttpGet("{index}/variants")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<MagicItem>> GetMagicItemVariants(string index)
        {
            var mainItem = _magicItems.FirstOrDefault(m => 
                m.Index?.Equals(index, StringComparison.OrdinalIgnoreCase) == true);
            
            if (mainItem == null)
            {
                return NotFound($"Magic item with index '{index}' not found");
            }

            if (mainItem.Variants == null || !mainItem.Variants.Any())
            {
                return Ok(new List<MagicItem>());
            }

            var variants = _magicItems
                .Where(m => mainItem.Variants.Any(v => 
                    v.Index?.Equals(m.Index, StringComparison.OrdinalIgnoreCase) == true))
                .ToList();
            
            return Ok(variants);
        }

        /// <summary>
        /// Get magic items that require attunement
        /// </summary>
        /// <returns>List of magic items that require attunement</returns>
        [HttpGet("attunement/required")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<MagicItem>> GetAttunementRequiredItems()
        {
            var items = _magicItems
                .Where(m => m.Desc?.Any(d => 
                    d.Contains("requires attunement", StringComparison.OrdinalIgnoreCase)) == true)
                .ToList();
            
            return Ok(items);
        }

        /// <summary>
        /// Get magic items that are cursed
        /// </summary>
        /// <returns>List of cursed magic items</returns>
        [HttpGet("cursed")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<MagicItem>> GetCursedItems()
        {
            var items = _magicItems
                .Where(m => m.Desc?.Any(d => 
                    d.Contains("curse", StringComparison.OrdinalIgnoreCase)) == true)
                .ToList();
            
            return Ok(items);
        }

        /// <summary>
        /// Get all available equipment categories
        /// </summary>
        /// <returns>List of unique equipment categories</returns>
        [HttpGet("categories")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<string>> GetEquipmentCategories()
        {
            var categories = _magicItems
                .Where(m => m.EquipmentCategory != null)
                .Select(m => m.EquipmentCategory.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
            
            return Ok(categories);
        }

        /// <summary>
        /// Get all available rarities
        /// </summary>
        /// <returns>List of unique rarities</returns>
        [HttpGet("rarities")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<string>> GetRarities()
        {
            var rarities = _magicItems
                .Where(m => m.Rarity != null)
                .Select(m => m.Rarity.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
            
            return Ok(rarities);
        }

        /// <summary>
        /// Get paginated magic items
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
        /// <returns>Paginated list of magic items</returns>
        [HttpGet("paginated")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<PaginatedResponse<MagicItem>> GetPaginatedMagicItems(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            if (page < 1)
            {
                return BadRequest("Page must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }

            var totalCount = _magicItems.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            var items = _magicItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new PaginatedResponse<MagicItem>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(response);
        }
    }
}