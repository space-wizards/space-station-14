using Content.Shared.Procedural.Corridors;
using Content.Shared.Procedural.Rooms;
using Content.Shared.Procedural.Walls;

namespace Content.Shared.Procedural.Dungeons;

[DataDefinition]
public sealed class BSPDunGen : IDungeonGenerator
{
    [DataField("bounds", required: true)] public Box2i Bounds;

    [DataField("min")]
    public Vector2i MinimumRoomDimensions = new(10, 10);

    [DataField("rooms")]
    public IRoomGen Rooms = new RandomWalkRoomGen();

    [DataField("corridors")]
    public ICorridorGen Corridors = new SimpleCorridorGen();

    [DataField("walls")]
    public WallGen Walls = new BoundaryWallGen();
}
