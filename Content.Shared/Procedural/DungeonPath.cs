namespace Content.Shared.Procedural;

/// <summary>
/// Connects 2 dungeon rooms.
/// </summary>
public sealed record DungeonPath(string Tile, string Wall, HashSet<Vector2i> Tiles)
{
    public string Tile = Tile;
    public string Wall = Wall;
}
