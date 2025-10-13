// Models/FeatureSpecific.cs
using System.Text.Json.Serialization;

namespace DndFeaturesApp.Models
{
    public class FeatureSpecific
    {
        [JsonPropertyName("expertise_options")]
        public OptionWrapper? ExpertiseOptions { get; set; }

        [JsonPropertyName("subfeature_options")]
        public OptionWrapper? SubfeatureOptions { get; set; }

        [JsonPropertyName("enemy_type_options")]
        public OptionWrapper? EnemyTypeOptions { get; set; }

        [JsonPropertyName("terrain_type_options")]
        public OptionWrapper? TerrainTypeOptions { get; set; }

        [JsonPropertyName("invocations")]
        public List<object>? Invocations { get; set; } // sometimes string, sometimes object
    }

    public class OptionWrapper
    {
        [JsonPropertyName("desc")]
        public string? Desc { get; set; }

        [JsonPropertyName("choose")]
        public int Choose { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public OptionSet? From { get; set; }
    }

    public class OptionSet
    {
        [JsonPropertyName("option_set_type")]
        public string OptionSetType { get; set; } = string.Empty;

        [JsonPropertyName("options")]
        public List<object> Options { get; set; } = new(); // flexible: string or object
    }

    public class Option
    {
        [JsonPropertyName("option_type")]
        public string OptionType { get; set; } = string.Empty;

        [JsonPropertyName("item")]
        public object? Item { get; set; } // sometimes object, sometimes null

        [JsonPropertyName("string")]
        public string? Value { get; set; }

        [JsonPropertyName("choice")]
        public object? Choice { get; set; } // flexible

        [JsonPropertyName("items")]
        public List<object>? Items { get; set; } // flexible
    }
}
