using System.Text.Json.Serialization;

namespace DndFeaturesApp.Models
{
    public class Language
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("typical_speakers")]
        public List<string> TypicalSpeakers { get; set; } = new();

        [JsonPropertyName("script")]
        public string? Script { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}
