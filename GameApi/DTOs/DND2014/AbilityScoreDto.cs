using System.Text.Json.Serialization;

namespace GameApi.DTOs.DND2014
{
    public class AbilityScore
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public List<string> Desc { get; set; } = new List<string>();

        [JsonPropertyName("skills")]
        public List<SkillReference> Skills { get; set; } = new List<SkillReference>();

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class SkillReference
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}
