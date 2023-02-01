using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Procedural;

[DataDefinition]
public sealed class DungeonConfig
{
    // TODO: Will likely need something more robust at some point.

    [DataField("tile", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = string.Empty;

    [DataField("wall", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Wall = string.Empty;
}
