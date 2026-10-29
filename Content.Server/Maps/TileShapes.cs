using System.Linq;
using Robust.Shared.Map;

namespace Content.Server.Maps;

public static class TileShapes
{
    /// <summary>
    /// Every tile of a <paramref name="width" /> w <paramref name="height" /> h rectangle with its corner at (0, 0).
    /// </summary>
    public static IEnumerable<(Vector2i, Tile)> Rectangle(int width, int height, int tileId)
    {
        return from w in Enumerable.Range(0, width)
            from h in Enumerable.Range(0, height)
            select (new Vector2i(w, h), new Tile(tileId));
    }
}
