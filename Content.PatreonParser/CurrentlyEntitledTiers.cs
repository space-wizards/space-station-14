using System.Text.Json.Serialization;

namespace Content.PatreonParser;

public sealed class CurrentlyEntitledTiers
{
    [JsonPropertyName("data")]
    public List<TierData> Data = default!;
}
