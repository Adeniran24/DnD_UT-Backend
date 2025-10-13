// Models/Race.cs
using System.Text.Json.Serialization;

namespace DnDAPI.Models
{
    public class Race
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("speed")]
        public int Speed { get; set; }

        [JsonPropertyName("ability_bonuses")]
        public List<AbilityBonus> AbilityBonuses { get; set; } = new();

        [JsonPropertyName("alignment")]
        public string Alignment { get; set; } = string.Empty;

        [JsonPropertyName("age")]
        public string Age { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public string Size { get; set; } = string.Empty;

        [JsonPropertyName("size_description")]
        public string SizeDescription { get; set; } = string.Empty;

        [JsonPropertyName("languages")]
        public List<LanguageReference> Languages { get; set; } = new();

        [JsonPropertyName("language_desc")]
        public string LanguageDesc { get; set; } = string.Empty;

        [JsonPropertyName("language_options")]
        public LanguageOptions? LanguageOptions { get; set; }

        [JsonPropertyName("ability_bonus_options")]
        public AbilityBonusOptions? AbilityBonusOptions { get; set; }

        [JsonPropertyName("traits")]
        public List<TraitReference> Traits { get; set; } = new();

        [JsonPropertyName("subraces")]
        public List<SubraceReference> Subraces { get; set; } = new();

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class AbilityBonus
    {
        [JsonPropertyName("ability_score")]
        public AbilityScore AbilityScore { get; set; } = new();

        [JsonPropertyName("bonus")]
        public int Bonus { get; set; }
    }

    public class AbilityScore
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class LanguageReference
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class TraitReference
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class SubraceReference
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class LanguageOptions
    {
        [JsonPropertyName("choose")]
        public int Choose { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public OptionSet From { get; set; } = new();
    }

    public class AbilityBonusOptions
    {
        [JsonPropertyName("choose")]
        public int Choose { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public OptionSet From { get; set; } = new();
    }

    public class OptionSet
    {
        [JsonPropertyName("option_set_type")]
        public string OptionSetType { get; set; } = string.Empty;

        [JsonPropertyName("options")]
        public List<Option> Options { get; set; } = new();
    }

    public class Option
    {
        [JsonPropertyName("option_type")]
        public string OptionType { get; set; } = string.Empty;

        [JsonPropertyName("ability_score")]
        public AbilityScore? AbilityScore { get; set; }

        [JsonPropertyName("bonus")]
        public int? Bonus { get; set; }

        [JsonPropertyName("item")]
        public LanguageReference? Item { get; set; }
    }
}