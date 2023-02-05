using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural;

[Prototype("dungeonConfig")]
public sealed class DungeonConfigPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("tile", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = string.Empty;

    [DataField("wall", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Wall = string.Empty;
}
