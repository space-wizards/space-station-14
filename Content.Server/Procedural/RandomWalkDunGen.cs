namespace Content.Server.Procedural;

[DataDefinition]
public sealed class RandomWalkDunGen : IDungeonGenerator
{
    [DataField("start")]
    public Vector2i StartPosition;

    [DataField("length")]
    public int Length;
}

public sealed record Dungeon
{
    public HashSet<Vector2i> Walls = new();
    public HashSet<Vector2i> Tiles = new();
}
