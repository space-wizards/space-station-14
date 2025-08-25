using System.Numerics;
using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.NPC;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="ExteriorDunGen"/>
    /// </summary>
    private async Task<List<Dungeon>> GenerateExteriorDungen(int runCount, int maxRuns, Vector2i position, ExteriorDunGen dungen, HashSet<Vector2i> reservedTiles, Random random)
    {
        DebugTools.Assert(_grid.ChunkCount > 0);

        var aabb = new Box2i(_grid.LocalAABB.BottomLeft.Floored(), _grid.LocalAABB.TopRight.Floored());
        // TODO: Cross-layer seeding. Need this because we need to be able to spread the dungeons out.
        var angle = new Random(_seed).NextAngle();
        var divisors = new Angle(Angle.FromDegrees(360) / maxRuns);

        // Offset each dungeon so they don't generate on top of each other.
        for (var i = 0; i < runCount; i++)
        {
            angle += (random.NextFloat(0.6f, 1.4f)) * divisors;
        }

        var distance = Math.Max(aabb.Width / 2f + 1f, aabb.Height / 2f + 1f);
        var startTile = new Vector2i(0, (int) distance).Rotate(angle);

        Vector2i? dungeonSpawn = null;

        // Gridcast
        SharedPathfindingSystem.GridCast(startTile, position, tile =>
        {
            if (!_maps.TryGetTileRef(_gridUid, _grid, tile, out var tileRef) ||
                _turf.IsSpace(tileRef.Tile))
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

        // Move it further in based on the spawn angle.
        if (dungen.Penetration.Y > 0)
        {
            var penetration = random.Next(dungen.Penetration.X, dungen.Penetration.Y);
            var diff = dungeonSpawn.Value - startTile;
            var diffVec = new Vector2(diff.X, diff.Y);
            dungeonSpawn = (diffVec.Normalized() * (penetration + diffVec.Length())).Floored() + startTile;
        }

        var subConfig = _prototype.Index(dungen.Proto);
        var nextSeed = random.Next();
        var dungeons = await GetDungeons(dungeonSpawn.Value, subConfig, subConfig.Layers, reservedTiles, nextSeed, new Random(nextSeed));

        return dungeons;
    }
}
