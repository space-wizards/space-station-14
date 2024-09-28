using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Maps;
using Content.Shared.NPC;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Robust.Shared.Collections;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="ExteriorDunGen"/>
    /// </summary>
    private async Task<List<Dungeon>> GenerateExteriorDungen(Vector2i position, ExteriorDunGen dungen, HashSet<Vector2i> reservedTiles, Random random)
    {
        DebugTools.Assert(_grid.ChunkCount > 0);

        var aabb = new Box2i(_grid.LocalAABB.BottomLeft.Floored(), _grid.LocalAABB.TopRight.Floored());
        var angle = random.NextAngle();

        var distance = Math.Max(aabb.Width / 2f + 1f, aabb.Height / 2f + 1f);

        var startTile = new Vector2i(0, (int) distance).Rotate(angle);

        Vector2i? dungeonSpawn = null;
        var pathfinder = _entManager.System<PathfindingSystem>();

        // Gridcast
        SharedPathfindingSystem.GridCast(startTile, position, tile =>
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
        var nextSeed = random.Next();
        var dungeons = await GetDungeons(dungeonSpawn.Value, config, config.Data, config.Layers, reservedTiles, nextSeed, new Random(nextSeed));

        return dungeons;
    }
}
