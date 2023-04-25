namespace Content.Shared.Procedural;

public sealed class Dungeon
{
    /// <summary>
    /// Starting position used to generate the dungeon from.
    /// </summary>
    public Vector2i Position;

    public Vector2i Center;

    public List<DungeonRoom> Rooms = new();

    /// <summary>
    /// Hashset of the tiles across all rooms.
    /// </summary>
    public HashSet<Vector2i> RoomTiles = new();
}
