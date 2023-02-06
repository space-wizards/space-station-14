using Content.Shared.Procedural.Walls;

namespace Content.Shared.Procedural.Dungeons;

[DataDefinition]
public sealed class RandomWalkDunGen : IDungeonGenerator
{
    [DataField("start")] public Vector2i StartPosition;

    [DataField("length")] public int Length = 10;

    [DataField("iterations")] public int Iterations = 10;

    [DataField("randomEachIteration")] public bool StartRandomlyEachIteration = true;

    [DataField("walls")] public WallGen Walls = new BoundaryWallGen();
}

public sealed record Dungeon
{
    public HashSet<Vector2i> AllTiles = new();
    public HashSet<Vector2i> Corridors = new();
    public List<DungeonRoom> Rooms = new();
    public HashSet<Vector2i> Walls = new();
}

public sealed record DungeonRoom
{
    public Vector2 Center;
    public HashSet<Vector2i> Tiles = new();
}
