using Content.Shared.Procedural.RoomLayouts;
using Content.Shared.Procedural.Walls;

namespace Content.Shared.Procedural.Rooms;

[DataDefinition]
public sealed class BSPRoomGen : IRoomGen
{
    [DataField("min")]
    public Vector2i MinimumRoomDimensions = new(10, 10);

    [DataField("rooms")]
    public IRoomLayout Rooms = new RandomWalkRoomLayout();

    [DataField("walls")]
    public WallGen Walls = new BoundaryWallGen();
}
