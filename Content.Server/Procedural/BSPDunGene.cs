using Content.Server.Procedural.Corridors;
using Content.Server.Procedural.Rooms;
using Content.Server.Procedural.Walls;

namespace Content.Server.Procedural;

[DataDefinition]
public sealed class BSPDunGen : IDungeonGenerator
{
    [DataField("bounds", required: true)] public Box2i Bounds;

    [DataField("min")]
    public Vector2i MinimumRoomDimensions = new(4, 4);

    [DataField("rooms")]
    public IRoomGen Rooms = new RandomWalkRoomGen();

    [DataField("corridors")]
    public ICorridorGen Corridors = new SimpleCorridorGen();

    [DataField("walls")]
    public IWallGen Walls = new BoundaryWallGen();
}
