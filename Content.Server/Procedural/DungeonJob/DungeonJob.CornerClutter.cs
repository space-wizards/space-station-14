using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Random;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="CornerClutterDunGen"/>
    /// </summary>
    private async Task PostGen(CornerClutterDunGen gen, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        var contentsTable = _prototype.Index(gen.Contents);

        foreach (var tile in dungeon.CorridorTiles)
        {
            if (reservedTiles.Contains(tile))
                continue;

            var blocked = _anchorable.TileFree(_grid, tile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask);

            if (blocked)
                continue;

            // If at least 2 adjacent tiles are blocked consider it a corner
            for (var i = 0; i < 4; i++)
            {
                var dir = (Direction) (i * 2);
                blocked = HasWall(tile + dir.ToIntVec());

                if (!blocked)
                    continue;

                var nextDir = (Direction) ((i + 1) * 2 % 8);
                blocked = HasWall(tile + nextDir.ToIntVec());

                if (!blocked)
                    continue;

                if (random.Prob(gen.Chance))
                {
                    var coords = _maps.GridTileToLocal(_gridUid, _grid, tile);
                    var protos = contentsTable.Table.GetSpawns(random, _entManager, _prototype);
                    _entManager.SpawnEntitiesAttachedTo(coords, protos);
                }

                break;
            }
        }
    }
}
