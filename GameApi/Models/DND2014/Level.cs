using System.Text.Json.Serialization;

namespace DndFeaturesApp.Models
{
    public class Level
    {
        [JsonPropertyName("level")]
        public int LevelNumber { get; set; }

        [JsonPropertyName("ability_score_bonuses")]
        public int AbilityScoreBonuses { get; set; }

        [JsonPropertyName("prof_bonus")]
        public int ProficiencyBonus { get; set; }

        [JsonPropertyName("features")]
        public List<FeatureReference> Features { get; set; } = new();

        [JsonPropertyName("class_specific")]
        public ClassSpecific? ClassSpecific { get; set; }

        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("class")]
        public ClassReference Class { get; set; } = new();

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class FeatureReference
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class ClassSpecific
    {
        [JsonPropertyName("rage_count")]
        public int RageCount { get; set; }

        [JsonPropertyName("rage_damage_bonus")]
        public int RageDamageBonus { get; set; }

        [JsonPropertyName("brutal_critical_dice")]
        public int BrutalCriticalDice { get; set; }
    }
}
