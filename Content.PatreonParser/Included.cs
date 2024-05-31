using System.Text.Json.Serialization;

namespace Content.PatreonParser;

public sealed class Included
{
    [JsonPropertyName("id")]
    public int Id;

    [JsonPropertyName("type")]
    public string Type = default!;

    [JsonPropertyName("attributes")]
    public Attributes Attributes = default!;
}
