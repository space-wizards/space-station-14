using Content.Server.Procedural.Walls;

namespace Content.Server.Procedural;

[DataDefinition]
public sealed class RandomWalkDunGen : IDungeonGenerator
{
    [DataField("start")]
    public Vector2i StartPosition;

    [DataField("length")]
    public int Length = 10;

    [DataField("iterations")]
    public int Iterations = 10;

    [DataField("randomEachIteration")]
    public bool StartRandomlyEachIteration = true;

    [DataField("walls")]
    public IWallGen Walls = new BoundaryWallGen();
}

public sealed record Dungeon
{
    public HashSet<Vector2i> Corridors = new();
    public List<DungeonRoom> Rooms = new();
    public HashSet<Vector2i> Walls = new();
}

public sealed record DungeonRoom
{
    public HashSet<Vector2i> Tiles = new();
}
