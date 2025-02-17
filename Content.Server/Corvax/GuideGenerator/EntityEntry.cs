using System.Text.Json.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Server.GuideGenerator;

public sealed class EntityEntry
{
    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("desc")]
    public string Description { get; }

    public EntityEntry(EntityPrototype proto)
    {
        Id = proto.ID;
        Name = TextTools.TextTools.CapitalizeString(proto.Name); // Corvax-Wiki
        Description = proto.Description;
    }
}
