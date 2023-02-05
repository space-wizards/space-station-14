using Content.Shared.Procedural.Walls;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    public HashSet<Vector2i> GetWalls(IWallGen gen, Box2i boundaries, HashSet<Vector2i> floors)
    {
        switch (gen)
        {
            case BoundaryWallGen boundary:
                return GetWalls(boundary, boundaries, floors);
            case FillWallGen fill:
                return GetWalls(fill, boundaries, floors);
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Gets the wall boundaries for the specified tiles. Doesn't return any floor tiles.
    /// </summary>
    public HashSet<Vector2i> GetWalls(BoundaryWallGen gen, Box2i boundaries, HashSet<Vector2i> floors)
    {
        var walls = new HashSet<Vector2i>(floors.Count);

        foreach (var tile in floors)
        {
            for (var i = 0; i < 8; i++)
            {
                var direction = (Direction) i;
                var neighborTile = tile + direction.ToIntVec();
                if (floors.Contains(neighborTile))
                    continue;

                walls.Add(neighborTile);
            }
        }

        return walls;
    }

    public HashSet<Vector2i> GetWalls(FillWallGen gen, Box2i boundaries, HashSet<Vector2i> floors)
    {
        var walls = new HashSet<Vector2i>((boundaries.Width + 2) * (boundaries.Height + 2) - floors.Count);

        for (var x = boundaries.Left - 1; x < boundaries.Right + 1; x++)
        {
            for (var y = boundaries.Bottom - 1; y < boundaries.Top + 1; y++)
            {
                var index = new Vector2i(x, y);
                if (floors.Contains(index))
                    continue;

                walls.Add(index);
            }
        }

        return walls;
    }
}
