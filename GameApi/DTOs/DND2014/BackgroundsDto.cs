using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameApi.DTOs.DND2014
{
    public class CharacterBackground
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("starting_proficiencies")]
        public List<BackgroundReference> StartingProficiencies { get; set; } = new();

        [JsonPropertyName("language_options")]
        public BackgroundOptionSet LanguageOptions { get; set; } = new();

        [JsonPropertyName("starting_equipment")]
        public List<BackgroundEquipmentItem> StartingEquipment { get; set; } = new();

        [JsonPropertyName("starting_equipment_options")]
        public List<BackgroundEquipmentOption> StartingEquipmentOptions { get; set; } = new();

        [JsonPropertyName("feature")]
        public BackgroundFeature Feature { get; set; } = new();

        [JsonPropertyName("personality_traits")]
        public BackgroundChoiceSet PersonalityTraits { get; set; } = new();

        [JsonPropertyName("ideals")]
        public BackgroundChoiceSet Ideals { get; set; } = new();

        [JsonPropertyName("bonds")]
        public BackgroundChoiceSet Bonds { get; set; } = new();

        [JsonPropertyName("flaws")]
        public BackgroundChoiceSet Flaws { get; set; } = new();

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class BackgroundReference
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class BackgroundOptionSet
    {
        [JsonPropertyName("choose")]
        public int Choose { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public BackgroundOptionFrom From { get; set; } = new();
    }

    public class BackgroundOptionFrom
    {
        [JsonPropertyName("option_set_type")]
        public string OptionSetType { get; set; } = string.Empty;

        [JsonPropertyName("resource_list_url")]
        public string ResourceListUrl { get; set; } = string.Empty;

        [JsonPropertyName("options")]
        public List<BackgroundChoiceOption> Options { get; set; } = new();
    }

    public class BackgroundChoiceOption
    {
        [JsonPropertyName("option_type")]
        public string OptionType { get; set; } = string.Empty;

        [JsonPropertyName("string")]
        public string String { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public string Desc { get; set; } = string.Empty;

        [JsonPropertyName("alignments")]
        public List<BackgroundReference> Alignments { get; set; } = new();
    }

    public class BackgroundEquipmentItem
    {
        [JsonPropertyName("equipment")]
        public BackgroundReference Equipment { get; set; } = new();

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    public class BackgroundEquipmentOption
    {
        [JsonPropertyName("choose")]
        public int Choose { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public BackgroundOptionFrom From { get; set; } = new();
    }

    public class BackgroundFeature
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public List<string> Desc { get; set; } = new();
    }

    public class BackgroundChoiceSet
    {
        [JsonPropertyName("choose")]
        public int Choose { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public BackgroundOptionFrom From { get; set; } = new();
    }
}
