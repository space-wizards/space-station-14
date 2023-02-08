namespace Content.Shared.Procedural;

public sealed record Dungeon
{
    public HashSet<Vector2i> AllTiles = new();
    public List<DungeonPath> Paths = new();
    public List<DungeonRoom> Rooms = new();
    public HashSet<Vector2i> Walls = new();
}
