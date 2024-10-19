using System.Numerics;
using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Map;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="CorridorDunGen"/>
    /// </summary>
    private async Task PostGen(CorridorDunGen gen, DungeonData data, Dungeon dungeon, HashSet<Vector2i> reservedTiles, Random random)
    {
        if (!data.Tiles.TryGetValue(DungeonDataKey.FallbackTile, out var tileProto))
        {
            LogDataError(typeof(CorridorDunGen));
            return;
        }

        var entrances = new List<Vector2i>(dungeon.Rooms.Count);

        // Grab entrances
        foreach (var room in dungeon.Rooms)
        {
            entrances.AddRange(room.Entrances);
        }

        var edges = _dungeon.MinimumSpanningTree(entrances, random);
        await SuspendDungeon();

        if (!ValidateResume())
            return;

        // TODO: Add in say 1/3 of edges back in to add some cyclic to it.

        var expansion = gen.Width - 2;
        // Okay so tl;dr is that we don't want to cut close to rooms as it might go from 3 width to 2 width suddenly
        // So we will add a buffer range around each room to deter pathfinding there unless necessary
        var deterredTiles = new HashSet<Vector2i>();

        if (expansion >= 1)
        {
            foreach (var tile in dungeon.RoomExteriorTiles)
            {
                for (var x = -expansion; x <= expansion; x++)
                {
                    for (var y = -expansion; y <= expansion; y++)
                    {
                        var neighbor = new Vector2(tile.X + x, tile.Y + y).Floored();

                        if (dungeon.RoomTiles.Contains(neighbor) ||
                            dungeon.RoomExteriorTiles.Contains(neighbor) ||
                            entrances.Contains(neighbor))
                        {
                            continue;
                        }

                        deterredTiles.Add(neighbor);
                    }
                }
            }
        }

        foreach (var room in dungeon.Rooms)
        {
            foreach (var entrance in room.Entrances)
            {
                // Just so we can still actually get in to the entrance we won't deter from a tile away from it.
                var normal = (entrance + _grid.TileSizeHalfVector - room.Center).ToWorldAngle().GetCardinalDir().ToIntVec();
                deterredTiles.Remove(entrance + normal);
            }
        }

        var excludedTiles = new HashSet<Vector2i>(dungeon.RoomExteriorTiles);
        excludedTiles.UnionWith(dungeon.RoomTiles);
        var corridorTiles = new HashSet<Vector2i>();

        _dungeon.GetCorridorNodes(corridorTiles, edges, gen.PathLimit, excludedTiles, tile =>
        {
            var mod = 1f;

            if (corridorTiles.Contains(tile))
            {
                mod *= 0.1f;
            }

            if (deterredTiles.Contains(tile))
            {
                mod *= 2f;
            }

            return mod;
        });

        WidenCorridor(dungeon, gen.Width, corridorTiles);

        var setTiles = new List<(Vector2i, Tile)>();
        var tileDef = (ContentTileDefinition) _tileDefManager[tileProto];

        foreach (var tile in corridorTiles)
        {
            if (reservedTiles.Contains(tile))
                continue;

            setTiles.Add((tile, _tile.GetVariantTile(tileDef, random)));
        }

        _maps.SetTiles(_gridUid, _grid, setTiles);
        dungeon.CorridorTiles.UnionWith(corridorTiles);
        dungeon.RefreshAllTiles();
        BuildCorridorExterior(dungeon);
    }
}
