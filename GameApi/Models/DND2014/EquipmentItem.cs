using System.Text.Json.Serialization;

namespace GameApi.Models.DND2014
{
    public class EquipmentItem
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("equipment_category")]
        public EquipmentCategory? EquipmentCategory { get; set; }

        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }

        [JsonPropertyName("cost")]
        public Cost? Cost { get; set; }

        [JsonPropertyName("desc")]
        public List<string>? Description { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        // Weapon properties
        [JsonPropertyName("weapon_category")]
        public string? WeaponCategory { get; set; }

        [JsonPropertyName("weapon_range")]
        public string? WeaponRange { get; set; }

        [JsonPropertyName("category_range")]
        public string? CategoryRange { get; set; }

        [JsonPropertyName("damage")]
        public Damage? Damage { get; set; }

        [JsonPropertyName("range")]
        public WeaponRangeValue? Range { get; set; }

        [JsonPropertyName("properties")]
        public List<WeaponProperty>? Properties { get; set; }

        // Armor properties
        [JsonPropertyName("armor_category")]
        public string? ArmorCategory { get; set; }

        [JsonPropertyName("armor_class")]
        public ArmorClass? ArmorClass { get; set; }

        [JsonPropertyName("str_minimum")]
        public int? StrMinimum { get; set; }

        [JsonPropertyName("stealth_disadvantage")]
        public bool? StealthDisadvantage { get; set; }

        // Gear properties
        [JsonPropertyName("gear_category")]
        public GearCategory? GearCategory { get; set; }

        [JsonPropertyName("quantity")]
        public int? Quantity { get; set; }

        // Tool properties
        [JsonPropertyName("tool_category")]
        public string? ToolCategory { get; set; }

        // Vehicle properties
        [JsonPropertyName("vehicle_category")]
        public string? VehicleCategory { get; set; }

        [JsonPropertyName("speed")]
        public Speed? Speed { get; set; }

        [JsonPropertyName("capacity")]
        public string? Capacity { get; set; }
    }

    public class EquipmentCategory
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class Cost
    {
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "gp";
    }

    public class Damage
    {
        [JsonPropertyName("damage_dice")]
        public string DamageDice { get; set; } = string.Empty;

        [JsonPropertyName("damage_type")]
        public DamageType? DamageType { get; set; }
    }

    public class DamageType
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class WeaponRangeValue
    {
        [JsonPropertyName("normal")]
        public int Normal { get; set; }

        [JsonPropertyName("long")]
        public int? Long { get; set; }
    }

    public class WeaponProperty
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class ArmorClass
    {
        [JsonPropertyName("base")]
        public int Base { get; set; }

        [JsonPropertyName("dex_bonus")]
        public bool DexBonus { get; set; }

        [JsonPropertyName("max_bonus")]
        public int? MaxBonus { get; set; }
    }

    public class GearCategory
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class Speed
    {
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = string.Empty;
    }
}