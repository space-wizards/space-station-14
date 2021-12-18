using System.Text.Json.Serialization;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GuideGenerator;

public class ReagentEntry
{
    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("group")]
    public string Group { get; }

    [JsonPropertyName("desc")]
    public string Description { get; }

    [JsonPropertyName("physicalDesc")]
    public string PhysicalDescription { get; }

    [JsonPropertyName("color")]
    public string SubstanceColor { get; }

    public ReagentEntry(ReagentPrototype proto)
    {
        Id = proto.ID;
        Name = proto.Name;
        Group = proto.Group;
        Description = proto.Description;
        PhysicalDescription = proto.PhysicalDescription;
        SubstanceColor = proto.SubstanceColor.ToHex();
    }
}
