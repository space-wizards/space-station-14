using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Robust.Shared.Map.Components;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="JunctionDunGen"/>
    /// </summary>
    private async Task PostGen(JunctionDunGen gen, DungeonData data, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        if (!data.Tiles.TryGetValue(DungeonDataKey.FallbackTile, out var tileProto) ||
            !data.SpawnGroups.TryGetValue(DungeonDataKey.Junction, out var junctionProto))
        {
            _sawmill.Error($"Dungeon data keys are missing for {nameof(gen)}");
            return;
        }

        var tileDef = _tileDefManager[tileProto];
        var entranceGroup = _prototype.Index(junctionProto);

        // N-wide junctions
        foreach (var tile in dungeon.CorridorTiles)
        {
            if (!_anchorable.TileFree(_grid, tile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                continue;

            // Check each direction:
            // - Check if immediate neighbors are free
            // - Check if the neighbors beyond that are not free
            // - Then check either side if they're slightly more free
            var exteriorWidth = (int) Math.Floor(gen.Width / 2f);
            var width = (int) Math.Ceiling(gen.Width / 2f);

            for (var i = 0; i < 2; i++)
            {
                var isValid = true;
                var neighborDir = (Direction) (i * 2);
                var neighborVec = neighborDir.ToIntVec();

                for (var j = -width; j <= width; j++)
                {
                    if (j == 0)
                        continue;

                    var neighbor = tile + neighborVec * j;

                    // If it's an end tile then check it's occupied.
                    if (j == -width ||
                        j == width)
                    {
                        if (!HasWall(neighbor))
                        {
                            isValid = false;
                            break;
                        }

                        continue;
                    }

                    // If we're not at the end tile then check it + perpendicular are free.
                    if (!_anchorable.TileFree(_grid, neighbor, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                    {
                        isValid = false;
                        break;
                    }

                    var perp1 = tile + neighborVec * j + ((Direction) ((i * 2 + 2) % 8)).ToIntVec();
                    var perp2 = tile + neighborVec * j + ((Direction) ((i * 2 + 6) % 8)).ToIntVec();

                    if (!_anchorable.TileFree(_grid, perp1, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                    {
                        isValid = false;
                        break;
                    }

                    if (!_anchorable.TileFree(_grid, perp2, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (!isValid)
                    continue;

                // Check corners to see if either side opens up (if it's just a 1x wide corridor do nothing, needs to be a funnel.
                foreach (var j in new [] {-exteriorWidth, exteriorWidth})
                {
                    var freeCount = 0;

                    // Need at least 3 of 4 free
                    for (var k = 0; k < 4; k++)
                    {
                        var cornerDir = (Direction) (k * 2 + 1);
                        var cornerVec = cornerDir.ToIntVec();
                        var cornerNeighbor = tile + neighborVec * j + cornerVec;

                        if (_anchorable.TileFree(_grid, cornerNeighbor, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                        {
                            freeCount++;
                        }
                    }

                    if (freeCount < gen.Width)
                        continue;

                    // Valid!
                    isValid = true;

                    for (var x = -width + 1; x < width; x++)
                    {
                        var weh = tile + neighborDir.ToIntVec() * x;

                        if (reservedTiles.Contains(weh))
                            continue;

                        _maps.SetTile(_gridUid, _grid, weh, _tile.GetVariantTile((ContentTileDefinition) tileDef, random));

                        var coords = _maps.GridTileToLocal(_gridUid, _grid, weh);
                        _entManager.SpawnEntities(coords, EntitySpawnCollection.GetSpawns(entranceGroup.Entries, random));
                    }

                    break;
                }

                if (isValid)
                {
                    await SuspendDungeon();

                    if (!ValidateResume())
                        return;
                }

                break;
            }
        }
    }
}
