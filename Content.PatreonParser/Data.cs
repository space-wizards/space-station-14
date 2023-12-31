using System.Text.Json.Serialization;

namespace Content.PatreonParser;

public sealed class Data
{
    [JsonPropertyName("id")]
    public string Id = default!;

    [JsonPropertyName("type")]
    public string Type = default!;

    [JsonPropertyName("attributes")]
    public Attributes Attributes = default!;

    [JsonPropertyName("relationships")]
    public Relationships Relationships = default!;
}
