using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural.Rooms;

[ImplicitDataDefinitionForInheritors]
public abstract class RoomGen
{
    [DataField("wall", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Wall = string.Empty;

    [DataField("tile", customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = string.Empty;
}
