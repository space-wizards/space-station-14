namespace Content.Shared.Procedural;

public sealed class Dungeon
{
    public readonly List<DungeonRoom> Rooms;

    /// <summary>
    /// Hashset of the tiles across all rooms.
    /// </summary>
    public readonly HashSet<Vector2i> RoomTiles = new();

    public readonly HashSet<Vector2i> RoomExteriorTiles = new();

    public readonly HashSet<Vector2i> CorridorTiles = new();

    public readonly HashSet<Vector2i> CorridorExteriorTiles = new();

    public readonly HashSet<Vector2i> Entrances = new();

    public Dungeon()
    {
        Rooms = new List<DungeonRoom>();
    }

    public Dungeon(List<DungeonRoom> rooms)
    {
        Rooms = rooms;

        foreach (var room in Rooms)
        {
            Entrances.UnionWith(room.Entrances);
        }
    }
}
