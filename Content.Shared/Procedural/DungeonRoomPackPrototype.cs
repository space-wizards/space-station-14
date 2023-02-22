using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural;

[Prototype("dungeonRoomPack")]
public sealed class DungeonRoomPackPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    /// <summary>
    /// Used to associate the room pack with other room packs with the same dimensions.
    /// </summary>
    [DataField("size", required: true)]
    public Vector2i Size;

    [DataField("rooms", required: true)]
    public List<Box2i> Rooms = new();

    // TODO: Need a test to ensure no duplicates.
    /// <summary>
    /// Connections between internal rooms
    /// </summary>
    [DataField("roomConnections")]
    public List<Vector2i> RoomConnections = new();

    /// <summary>
    /// Where our connections are to external packs.
    /// </summary>
    [DataField("connections")] public List<Vector2i> Connections = new();
}
