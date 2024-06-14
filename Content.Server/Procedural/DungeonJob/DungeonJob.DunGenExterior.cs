using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="ExteriorDunGen"/>
    /// </summary>
    private async Task<List<Dungeon>> GenerateExteriorDungeon(Vector2i position, DungeonData data, ExteriorDunGen dungen, HashSet<Vector2i> reservedTiles, int seed)
    {
        DebugTools.Assert(_grid.ChunkCount > 0);

        var rand = new Random(seed);
        var aabb = new Box2i(_grid.LocalAABB.BottomLeft.Floored(), _grid.LocalAABB.TopRight.Floored());

        var index = rand.Next((int) (aabb.Width * 2f + aabb.Height * 2f) + 1);
        Vector2i startTile;

        if (index < aabb.Width)
        {
            startTile = new Vector2i(index, aabb.Bottom - 1);
        }
        else if (index < aabb.Width * 2)
        {
            startTile = new Vector2i(index - aabb.Width, aabb.Top + 1);
        }
        else if (index < aabb.Width * 2 + aabb.Height)
        {
            startTile = new Vector2i(aabb.Left - 1, index - aabb.Width * 2 + aabb.Height);
        }
        else
        {
            startTile = new Vector2i(aabb.Right + 1, index - aabb.Width * 2 + aabb.Height);
        }

        Vector2i? dungeonSpawn = null;

        // Gridcast
        GridCast(startTile, position, tile =>
        {
            if (!_maps.TryGetTileRef(_gridUid, _grid, tile, out var tileRef) ||
                tileRef.Tile.IsSpace(_tileDefManager))
            {
                return true;
            }

            dungeonSpawn = tile;
            return false;
        });

        if (dungeonSpawn == null)
        {
            return new List<Dungeon>()
            {
                Dungeon.Empty
            };
        }

        var config = _prototype.Index(dungen.Proto);
        var dungeons = await GetDungeons(dungeonSpawn.Value, config, config.Data, config.Layers, reservedTiles, seed);

        return dungeons;
    }

    public static void GridCast(Vector2i start, Vector2i end, Vector2iCallback callback)
    {
        // https://gist.github.com/Pyr3z/46884d67641094d6cf353358566db566
        // declare all locals at the top so it's obvious how big the footprint is
        int dx, dy, xinc, yinc, side, i, error;

        // starting cell is always returned
        if (!callback(start))
            return;

        xinc  = (end.X < start.X) ? -1 : 1;
        yinc  = (end.Y < start.Y) ? -1 : 1;
        dx    = xinc * (end.X - start.X);
        dy    = yinc * (end.Y - start.Y);
        var ax = start.X;
        var ay = start.Y;

        if (dx == dy) // Handle perfect diagonals
        {
            // I include this "optimization" for more aesthetic reasons, actually.
            // While Bresenham's Line can handle perfect diagonals just fine, it adds
            // additional cells to the line that make it not a perfect diagonal
            // anymore. So, while this branch is ~twice as fast as the next branch,
            // the real reason it is here is for style.

            // Also, there *is* the reason of performance. If used for cell-based
            // raycasts, for example, then perfect diagonals will check half as many
            // cells.

            while (dx --> 0)
            {
                ax += xinc;
                ay += yinc;
                if (!callback(new Vector2i(ax, ay)))
                    return;
            }

            return;
        }

        // Handle all other lines

        side = -1 * ((dx == 0 ? yinc : xinc) - 1);

        i     = dx + dy;
        error = dx - dy;

        dx *= 2;
        dy *= 2;

        while (i --> 0)
        {
            if (error > 0 || error == side)
            {
                ax    += xinc;
                error -= dy;
            }
            else
            {
                ay    += yinc;
                error += dx;
            }

            if (!callback(new Vector2i(ax, ay)))
                return;
        }
    }

    public delegate bool Vector2iCallback(Vector2i index);
}
