using System.Linq;

namespace Content.Shared.Procedural;

public sealed record DungeonRoom(HashSet<Vector2i> Tiles)
{
    public Vector2 Center
    {
        get
        {
            if (_center != null)
                return _center.Value;

            var center = Vector2.Zero;

            foreach (var pos in Tiles)
            {
                center += pos;
            }

            if (Tiles.Count > 0)
                center /= Tiles.Count;

            _center = center;
            return _center.Value;
        }
    }

    private Vector2? _center;
}
