namespace GameApi.Models
{
    public class EquipmentCategory
    {
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class Rarity
    {
        public string Name { get; set; } = string.Empty;
    }

    public class Variant
    {
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class MagicItem
    {
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public EquipmentCategory EquipmentCategory { get; set; } = new EquipmentCategory();
        public Rarity Rarity { get; set; } = new Rarity();
        public List<Variant> Variants { get; set; } = new List<Variant>();
        public bool Variant { get; set; }
        public List<string> Desc { get; set; } = new List<string>();
        public string Image { get; set; } = string.Empty;
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