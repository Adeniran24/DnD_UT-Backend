using System.Text.Json.Serialization;

namespace GameApi.Models
{
    public class EquipmentCategory
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class Rarity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class Variant
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class MagicItem
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("equipment_category")]
        public EquipmentCategory EquipmentCategory { get; set; } = new EquipmentCategory();

        [JsonPropertyName("rarity")]
        public Rarity Rarity { get; set; } = new Rarity();

        [JsonPropertyName("variants")]
        public List<Variant> Variants { get; set; } = new List<Variant>();

        [JsonPropertyName("variant")]
        public bool Variant { get; set; }

        [JsonPropertyName("desc")]
        public List<string> Desc { get; set; } = new List<string>();

        [JsonPropertyName("image")]
        public string Image { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}