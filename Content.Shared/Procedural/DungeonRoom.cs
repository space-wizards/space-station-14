namespace Content.Shared.Procedural;

public sealed record DungeonRoom(string Tile, string Wall, HashSet<Vector2i> Tiles, HashSet<Vector2i> Walls)
{
    public string Tile = Tile;
    public string Wall = Wall;
}
