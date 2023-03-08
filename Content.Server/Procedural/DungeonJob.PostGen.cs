using System.Linq;
using System.Threading.Tasks;
using Content.Server.Parallax;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

public sealed partial class DungeonJob
{
    /*
     * Run after the main dungeon generation
     */

    private async Task PostGen(BoundaryWallPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid, Random random)
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
        for (var i = 0; i < tiles.Count; i++)
        {
            var index = tiles[i];
            var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(index.Index);

            // Occupied tile.
            if (anchoredEnts.MoveNext(out _))
                continue;

            _entManager.SpawnEntity(gen.Wall, grid.GridTileToLocal(index.Index));

            if (i % 10 == 0)
            {
                await SuspendIfOutOfTime();
                ValidateResume();
            }
        }
    }

    private async Task PostGen(EntrancePostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid, Random random)
    {
        var rooms = new List<DungeonRoom>(dungeon.Rooms);
        var roomTiles = new List<Vector2i>();
        var tileData = new Tile(_tileDefManager[gen.Tile].TileId);
        var count = gen.Count;

        while (count > 0 && rooms.Count > 0)
        {
            var roomIndex = random.Next(rooms.Count);
            var room = rooms[roomIndex];
            rooms.RemoveAt(roomIndex);

            // Move out 3 tiles in a direction away from center of the room
            // If none of those intersect another tile it's probably external
            // TODO: Maybe need to take top half of furthest rooms in case there's interior exits?
            roomTiles.AddRange(room.Tiles);
            random.Shuffle(roomTiles);

            foreach (var tile in roomTiles)
            {
                // Check the interior node is at least accessible?
                // Can't do anchored because it might be a locker or something.
                // TODO: Better collision mask check
                if (_lookup.GetEntitiesIntersecting(gridUid, tile, LookupFlags.Dynamic | LookupFlags.Static).Any())
                    continue;

                var direction = (tile - room.Center).ToAngle().GetCardinalDir().ToAngle().ToVec();
                var isValid = true;

                for (var j = 0; j < 4; j++)
                {
                    var neighbor = (tile + direction).Floored();

                    // If it's an interior tile or blocked.
                    if (dungeon.RoomTiles.Contains(neighbor) || grid.GetAnchoredEntitiesEnumerator(neighbor).MoveNext(out _))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (!isValid)
                    continue;

                var entrancePos = (tile + direction).Floored();

                // Entrance wew
                grid.SetTile(entrancePos, tileData);
                var gridCoords = grid.GridTileToLocal(entrancePos);
                // Need to offset the spawn to avoid spawning in the room.
                _entManager.SpawnEntity(gen.Door, gridCoords);
                count--;

                // Clear out any biome tiles nearby to avoid blocking it
                foreach (var nearTile in grid.GetTilesIntersecting(new Circle(gridCoords.Position, 1.5f), false))
                {
                    if (dungeon.RoomTiles.Contains(nearTile.GridIndices))
                        continue;

                    grid.SetTile(nearTile.GridIndices, tileData);
                }

                break;
            }

            roomTiles.Clear();
        }
    }

    private async Task PostGen(MiddleConnectionPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid, Random random)
    {
        // TODO: Need a minimal spanning tree version tbh

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
                        _entManager.SpawnEntity(ent, gridPos);
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
