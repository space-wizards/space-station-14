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
    [DataField("rooms", required: true)] public List<RoomGen> Rooms = new();

    /// <summary>
    /// Path generators between rooms.
    /// </summary>
    [DataField("paths")] public List<PathGen> Paths = new();
}
