using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameApi.DTOs.DND2014
{
    public class DamageType
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public List<string> Desc { get; set; } = new();

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}
