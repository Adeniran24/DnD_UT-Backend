// Models/Feature.cs
using System.Text.Json.Serialization;

namespace DndFeaturesApp.Models
{
    public class Feature
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("class")]
        public ClassReference Class { get; set; } = new();

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("prerequisites")]
        public List<object> Prerequisites { get; set; } = new(); // can be string or object

        [JsonPropertyName("desc")]
        public List<string> Desc { get; set; } = new();

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("subclass")]
        public SubclassReference? Subclass { get; set; }

        [JsonPropertyName("feature_specific")]
        public FeatureSpecific? FeatureSpecific { get; set; }

        [JsonPropertyName("parent")]
        public ParentReference? Parent { get; set; }
    }

    public class ClassReference
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class SubclassReference
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class ParentReference
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}
