using Content.Shared.Procedural.Walls;

namespace Content.Shared.Procedural.Rooms;

[DataDefinition]
public sealed class RandomWalkRoomGen : RoomGen
{
    [DataField("start")] public Vector2i StartPosition;

    [DataField("length")] public int Length = 10;

    [DataField("iterations")] public int Iterations = 10;

    [DataField("randomEachIteration")] public bool StartRandomlyEachIteration = true;

    [DataField("walls")] public WallGen Walls = new BoundaryWallGen();
}
