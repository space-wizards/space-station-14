using Content.Shared.Procedural.Paths;
using Content.Shared.Procedural.RoomGens;
using Robust.Shared.Prototypes;

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
    [ViewVariables(VVAccess.ReadWrite), DataField("rooms", required: true)]
    public List<RoomGen> Rooms = new();

    /// <summary>
    /// Path generators between rooms.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("paths")]
    public List<PathGen> Paths = new();
}
