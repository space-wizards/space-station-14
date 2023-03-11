namespace Content.Shared.Procedural;

public sealed class Dungeon
{
    public List<DungeonRoom> Rooms = new();

    /// <summary>
    /// Hashset of the tiles across all rooms.
    /// </summary>
    public HashSet<Vector2i> RoomTiles = new();
}
