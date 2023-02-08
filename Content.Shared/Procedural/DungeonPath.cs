namespace Content.Shared.Procedural;

/// <summary>
/// Connects 2 dungeon rooms.
/// </summary>
public sealed record DungeonPath(HashSet<Vector2i> Tiles);
