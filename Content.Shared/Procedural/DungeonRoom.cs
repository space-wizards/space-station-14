namespace Content.Shared.Procedural;

public sealed record DungeonRoom(HashSet<Vector2i> Tiles, Vector2 Center, HashSet<Vector2i> Exterior)
{
    /// <summary>
    /// Nodes adjacent to tiles, including the corners.
    /// </summary>
    public readonly HashSet<Vector2i> Exterior = Exterior;
}
