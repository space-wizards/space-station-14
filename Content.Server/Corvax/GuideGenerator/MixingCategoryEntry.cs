using Content.Shared.Chemistry.Reaction;
using System.Text.Json.Serialization;

namespace Content.Server.Corvax.GuideGenerator;

public sealed class MixingCategoryEntry
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("id")]
    public string Id { get; }

    public MixingCategoryEntry(MixingCategoryPrototype proto)
    {
        Name = Loc.GetString(proto.VerbText);
        Id = proto.ID;
    }
}
