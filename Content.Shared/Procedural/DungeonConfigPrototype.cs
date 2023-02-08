using Content.Shared.Maps;
using Content.Shared.Procedural.Paths;
using Content.Shared.Procedural.Rooms;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural;

[Prototype("dungeonConfig")]
public sealed class DungeonConfigPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    // Also per-dungeon data is radius and seed.

    /// <summary>
    /// Room generators
    /// </summary>
    [DataField("rooms", required: true)] public List<IRoomGen> Rooms = new();

    /// <summary>
    /// Path generators between rooms.
    /// </summary>
    [DataField("paths")] public List<IPathGen> Paths = new();

    [DataField("tile", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = string.Empty;

    [DataField("wall", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Wall = string.Empty;
}
