using System.Text.Json.Serialization;

namespace Content.PatreonParser;

public sealed class TierData
{
    [JsonPropertyName("id")]
    public int Id;

    [JsonPropertyName("type")]
    public string Type = default!;
}
