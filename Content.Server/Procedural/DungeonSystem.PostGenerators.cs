using System.Linq;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    /*
     * Run after the main dungeon generation
     */

    private void PostGen(BoundaryWallPostGen gen, Dungeon dungeon, MapGridComponent grid, Random random)
    {
        var tile = new Tile(_tileDefManager[gen.Tile].TileId);
        var tiles = new List<(Vector2i Index, Tile Tile)>();

        // Spawn wall outline
        // - Tiles first
        foreach (var room in dungeon.Rooms)
        {
            foreach (var index in room.Tiles)
            {
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        var neighbor = new Vector2i(x + index.X, y + index.Y);

                        if (dungeon.RoomTiles.Contains(neighbor))
                            continue;

                        var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(neighbor);

                        // Occupied tile.
                        if (anchoredEnts.MoveNext(out _))
                            continue;

                        tiles.Add((neighbor, tile));
                    }
                }
            }
        }

        grid.SetTiles(tiles);

        // Double iteration coz we bulk set tiles for speed.
        foreach (var index in tiles)
        {
            var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(index.Index);

            // Occupied tile.
            if (anchoredEnts.MoveNext(out _))
                continue;

            Spawn(gen.Wall, grid.GridTileToLocal(index.Index));
        }
    }

    private void PostGen(EntrancePostGen gen, Dungeon dungeon, MapGridComponent grid, Random random)
    {
        // TODO:
    }

    private void PostGen(MiddleConnectionPostGen gen, Dungeon dungeon, MapGridComponent grid, Random random)
    {
        // TODO: split this out to triple / single gens and genericise it for entities.

        // Grab all of the room bounds
        // Then, work out connections between them
        var roomBorders = new Dictionary<DungeonRoom, HashSet<Vector2i>>(dungeon.Rooms.Count);

        foreach (var room in dungeon.Rooms)
        {
            var roomEdges = new HashSet<Vector2i>();

            foreach (var index in room.Tiles)
            {
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        // Cardinals only
                        if (x != 0 && y != 0 ||
                            x == 0 && y == 0)
                        {
                            continue;
                        }

                        var neighbor = new Vector2i(index.X + x, index.Y + y);

                        if (dungeon.RoomTiles.Contains(neighbor))
                            continue;

                        var anc = grid.GetAnchoredEntitiesEnumerator(neighbor);

                        // Occupied
                        if (anc.MoveNext(out _))
                            continue;

                        roomEdges.Add(neighbor);
                    }
                }
            }

            roomBorders.Add(room, roomEdges);
        }

        // Do pathfind from first room to work out graph.
        // TODO: Optional loops

        var roomConnections = new Dictionary<DungeonRoom, List<DungeonRoom>>();
        var frontier = new Queue<DungeonRoom>();
        frontier.Enqueue(dungeon.Rooms.First());
        var tile = new Tile(_tileDefManager[gen.Tile].TileId);

        while (frontier.TryDequeue(out var room))
        {
            var conns = roomConnections.GetOrNew(room);
            var border = roomBorders[room];

            foreach (var (otherRoom, otherBorders) in roomBorders)
            {
                if (room.Equals(otherRoom) ||
                    conns.Contains(otherRoom))
                {
                    continue;
                }

                var flipp = new HashSet<Vector2i>(border);
                flipp.IntersectWith(otherBorders);

                if (flipp.Count == 0 ||
                    gen.OverlapCount != -1 && flipp.Count != gen.OverlapCount)
                    continue;

                var center = Vector2.Zero;

                foreach (var node in flipp)
                {
                    center += (Vector2) node + grid.TileSize / 2f;
                }

                center /= flipp.Count;
                // Weight airlocks towards center more.
                var nodeDistances = new List<(Vector2i Node, float Distance)>(flipp.Count);

                foreach (var node in flipp)
                {
                    nodeDistances.Add((node, ((Vector2) node + grid.TileSize / 2f - center).LengthSquared));
                }

                nodeDistances.Sort((x, y) => x.Distance.CompareTo(y.Distance));

                var width = gen.Count;

                for (var i = 0; i < nodeDistances.Count; i++)
                {
                    var node = nodeDistances[i].Node;
                    var gridPos = grid.GridTileToLocal(node);
                    var anc = grid.GetAnchoredEntitiesEnumerator(node);

                    // Occupado
                    if (anc.MoveNext(out _))
                        continue;

                    width--;
                    grid.SetTile(node, tile);

                    foreach (var ent in gen.Entities)
                    {
                        Spawn(ent, gridPos);
                    }

                    if (width == 0)
                        break;
                }

                conns.Add(otherRoom);
                var otherConns = roomConnections.GetOrNew(otherRoom);
                otherConns.Add(room);
                frontier.Enqueue(otherRoom);
            }
        }
    }
}
