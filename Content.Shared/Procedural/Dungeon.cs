namespace Content.Shared.Procedural;

/// <summary>
/// Procedurally generated dungeon data.
/// </summary>
public sealed class Dungeon
{
    public static Dungeon Empty = new Dungeon();

    private List<DungeonRoom> _rooms;
    private HashSet<Vector2i> _allTiles = new();

    public IReadOnlyList<DungeonRoom> Rooms => _rooms;

    /// <summary>
    /// Hashset of the tiles across all rooms.
    /// </summary>
    public readonly HashSet<Vector2i> RoomTiles = new();

    public readonly HashSet<Vector2i> RoomExteriorTiles = new();

    public readonly HashSet<Vector2i> CorridorTiles = new();

    public readonly HashSet<Vector2i> CorridorExteriorTiles = new();

    public readonly HashSet<Vector2i> Entrances = new();

    public IReadOnlySet<Vector2i> AllTiles => _allTiles;

    public Dungeon() : this(new List<DungeonRoom>())
    {
    }

    public Dungeon(List<DungeonRoom> rooms)
    {
        // This reftype is mine now.
        _rooms = rooms;

        foreach (var room in _rooms)
        {
            InternalAddRoom(room);
        }

        RefreshAllTiles();
    }

    public void RefreshAllTiles()
    {
        _allTiles.Clear();
        _allTiles.UnionWith(RoomTiles);
        _allTiles.UnionWith(RoomExteriorTiles);
        _allTiles.UnionWith(CorridorTiles);
        _allTiles.UnionWith(CorridorExteriorTiles);
        _allTiles.UnionWith(Entrances);
    }

    public void Rebuild()
    {
        _allTiles.Clear();

        RoomTiles.Clear();
        RoomExteriorTiles.Clear();
        Entrances.Clear();

        foreach (var room in _rooms)
        {
            InternalAddRoom(room, false);
        }

        RefreshAllTiles();
    }

    public void AddRoom(DungeonRoom room)
    {
        _rooms.Add(room);
        InternalAddRoom(room);
    }

    private void InternalAddRoom(DungeonRoom room, bool refreshAll = true)
    {
        Entrances.UnionWith(room.Entrances);
        RoomTiles.UnionWith(room.Tiles);
        RoomExteriorTiles.UnionWith(room.Exterior);

        if (refreshAll)
            RefreshAllTiles();
    }
}
