using System.Text.Json.Serialization;

namespace Content.PatreonParser;

public sealed class Root
{
    [JsonPropertyName("data")]
    public Data Data = default!;

    [JsonPropertyName("included")]
    public List<Included> Included = default!;
}
