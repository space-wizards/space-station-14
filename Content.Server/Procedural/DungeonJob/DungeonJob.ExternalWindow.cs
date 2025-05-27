using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    // (Comment refers to internal & external).

    /*
     * You may be wondering why these are different.
     * It's because for internals we want to force it as it looks nicer and not leave it up to chance.
     */

    // TODO: Can probably combine these a bit, their differences are in really annoying to pull out spots.

    /// <summary>
    /// <see cref="ExternalWindowDunGen"/>
    /// </summary>
    private async Task PostGen(ExternalWindowDunGen gen, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        // Iterate every tile with N chance to spawn windows on that wall per cardinal dir.
        var chance = 0.25 / 3f;

        var allExterior = new HashSet<Vector2i>(dungeon.CorridorExteriorTiles);
        allExterior.UnionWith(dungeon.RoomExteriorTiles);
        var validTiles = allExterior.ToList();
        random.Shuffle(validTiles);

        var tiles = new List<(Vector2i, Tile)>();
        var tileDef = _tileDefManager[gen.Tile];
        var count = Math.Floor(validTiles.Count * chance);
        var index = 0;
        var takenTiles = new HashSet<Vector2i>();

        // There's a bunch of shit here but tl;dr
        // - don't spawn over cap
        // - Check if we have 3 tiles in a row that aren't corners and aren't obstructed
        foreach (var tile in validTiles)
        {
            if (index > count)
                break;

            // Room tile / already used.
            if (!_anchorable.TileFree(_grid, tile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask) ||
                takenTiles.Contains(tile))
            {
                continue;
            }

            // Check we're not on a corner
            for (var i = 0; i < 2; i++)
            {
                var dir = (Direction) (i * 2);
                var dirVec = dir.ToIntVec();
                var isValid = true;

                // Check 1 beyond either side to ensure it's not a corner.
                for (var j = -1; j < 4; j++)
                {
                    var neighbor = tile + dirVec * j;

                    if (!allExterior.Contains(neighbor) ||
                        takenTiles.Contains(neighbor) ||
                        !_anchorable.TileFree(_grid, neighbor, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                    {
                        isValid = false;
                        break;
                    }

                    // Also check perpendicular that it is free
                    foreach (var k in new [] {2, 6})
                    {
                        var perp = (Direction) ((i * 2 + k) % 8);
                        var perpVec = perp.ToIntVec();
                        var perpTile = tile + perpVec;

                        if (allExterior.Contains(perpTile) ||
                            takenTiles.Contains(neighbor) ||
                            !_anchorable.TileFree(_grid, perpTile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                        {
                            isValid = false;
                            break;
                        }
                    }

                    if (!isValid)
                        break;
                }

                if (!isValid)
                    continue;

                for (var j = 0; j < 3; j++)
                {
                    var neighbor = tile + dirVec * j;

                    if (reservedTiles.Contains(neighbor))
                        continue;

                    tiles.Add((neighbor, _tile.GetVariantTile((ContentTileDefinition) tileDef, random)));
                    index++;
                    takenTiles.Add(neighbor);
                }
            }
        }

        _maps.SetTiles(_gridUid, _grid, tiles);
        var contents = _prototype.Index(gen.Contents);

        foreach (var tile in tiles)
        {
            var gridPos = _maps.GridTileToLocal(_gridUid, _grid, tile.Item1);

            _entManager.SpawnEntitiesAttachedTo(gridPos, _entTable.GetSpawns(contents, random));
            await SuspendDungeon();

            if (!ValidateResume())
                return;
        }
    }
}
