using Content.Shared.Maps;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ShadowDimension;

public abstract class SharedShadowDimensionSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed record ShadowDimensionParams
{
    public int Seed;
    public Dictionary<ProtoId<TagPrototype>, EntProtoId> Replacements = new();
    public ProtoId<ContentTileDefinition> DefaultTile = "FloorChromite";
}
