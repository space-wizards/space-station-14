using System.Linq;

namespace Content.Shared.Procedural;

public sealed class Dungeon
{
    public HashSet<Vector2i> AllTiles
    {
        get
        {
            if (_allTiles != null)
                return _allTiles;

            _allTiles ??= new HashSet<Vector2i>();

            foreach (var path in Paths)
            {
                _allTiles.UnionWith(path.Tiles);
            }

            foreach (var room in Rooms)
            {
                _allTiles.UnionWith(room.Tiles);
            }

            return _allTiles;
        }
    }

    private HashSet<Vector2i>? _allTiles;

    public List<DungeonPath> Paths = new();
    public List<DungeonRoom> Rooms = new();
}
