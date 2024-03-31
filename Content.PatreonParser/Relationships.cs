using System.Text.Json.Serialization;

namespace Content.PatreonParser;

public sealed class Relationships
{
    [JsonPropertyName("currently_entitled_tiers")]
    public CurrentlyEntitledTiers CurrentlyEntitledTiers = default!;
}
