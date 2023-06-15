using System.Linq;
using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

public sealed partial class DungeonJob
{
    /*
     * Run after the main dungeon generation
     */

    private const int CollisionMask = (int) CollisionGroup.Impassable;
    private const int CollisionLayer = (int) CollisionGroup.Impassable;

    private async Task PostGen(BoundaryWallPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid, Random random)
    {
        var tile = new Tile(_tileDefManager[gen.Tile].TileId);
        var tiles = new List<(Vector2i Index, Tile Tile)>();

        // Spawn wall outline
        // - Tiles first
        foreach (var neighbor in dungeon.RoomExteriorTiles)
        {
            if (dungeon.RoomTiles.Contains(neighbor))
                continue;

            if (!_anchorable.TileFree(grid, neighbor, CollisionLayer, CollisionMask))
                continue;

            tiles.Add((neighbor, tile));
        }

        foreach (var neighbor in dungeon.CorridorExteriorTiles)
        {
            if (dungeon.RoomTiles.Contains(neighbor))
                continue;

            if (!_anchorable.TileFree(grid, neighbor, CollisionLayer, CollisionMask))
                continue;

            tiles.Add((neighbor, tile));
        }

        grid.SetTiles(tiles);

        // Double iteration coz we bulk set tiles for speed.
        for (var i = 0; i < tiles.Count; i++)
        {
            var index = tiles[i];
            if (!_anchorable.TileFree(grid, index.Index, CollisionLayer, CollisionMask))
                continue;

            // If no cardinal neighbors in dungeon then we're a corner.
            var isCorner = false;

            if (gen.CornerWall != null)
            {
                isCorner = true;

                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        if (x != 0 && y != 0)
                        {
                            continue;
                        }

                        var neighbor = new Vector2i(index.Index.X + x, index.Index.Y + y);

                        if (dungeon.RoomTiles.Contains(neighbor))
                        {
                            isCorner = false;
                            break;
                        }
                    }

                    if (!isCorner)
                        break;
                }

                if (isCorner)
                    _entManager.SpawnEntity(gen.CornerWall, grid.GridTileToLocal(index.Index));
            }

            if (!isCorner)
                _entManager.SpawnEntity(gen.Wall, grid.GridTileToLocal(index.Index));

            if (i % 10 == 0)
            {
                await SuspendIfOutOfTime();
                ValidateResume();
            }
        }
    }

    private async Task PostGen(DungeonEntrancePostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid, Random random)
    {
        var rooms = new List<DungeonRoom>(dungeon.Rooms);
        var roomTiles = new List<Vector2i>();
        var tileData = new Tile(_tileDefManager[gen.Tile].TileId);

        for (var i = 0; i < gen.Count; i++)
        {
            var roomIndex = random.Next(rooms.Count);
            var room = rooms[roomIndex];

            // Move out 3 tiles in a direction away from center of the room
            // If none of those intersect another tile it's probably external
            // TODO: Maybe need to take top half of furthest rooms in case there's interior exits?
            roomTiles.AddRange(room.Tiles);
            random.Shuffle(roomTiles);

            foreach (var tile in roomTiles)
            {
                var direction = (tile - room.Center).ToAngle().GetCardinalDir().ToAngle().ToVec();
                var isValid = true;

                for (var j = 1; j < 4; j++)
                {
                    var neighbor = (tile + direction * j).Floored();

                    // If it's an interior tile or blocked.
                    if (dungeon.RoomTiles.Contains(neighbor) || _lookup.GetEntitiesIntersecting(gridUid, neighbor, LookupFlags.Dynamic | LookupFlags.Static).Any())
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
                ClearDoor(dungeon, grid, entrancePos);
                var gridCoords = grid.GridTileToLocal(entrancePos);
                // Need to offset the spawn to avoid spawning in the room.

                foreach (var ent in gen.Entities)
                {
                    _entManager.SpawnEntity(ent, gridCoords);
                }

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

    private async Task PostGen(ExternalWindowPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid,
        Random random)
    {
        // Iterate every room with N chance to spawn windows on that wall per cardinal dir.
        var chance = 0.25;

        var allExterior = new HashSet<Vector2i>(dungeon.CorridorExteriorTiles);
        allExterior.UnionWith(dungeon.RoomExteriorTiles);
        var validTiles = allExterior.ToList();
        random.Shuffle(validTiles);

        var tiles = new List<(Vector2i, Tile)>();
        var tileId = new Tile(_tileDefManager[gen.Tile].TileId);
        var count = Math.Floor(validTiles.Count * chance);
        var index = 0;

        foreach (var tile in validTiles)
        {
            if (index > count)
                break;

            // Room tile / already used.
            if (dungeon.RoomTiles.Contains(tile) || !allExterior.Contains(tile))
                continue;

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    var neighbor = new Vector2i(tile.X + x, tile.Y + y);

                    if (!allExterior.Contains(neighbor))
                        continue;

                    if (!_anchorable.TileFree(grid, neighbor, CollisionLayer, CollisionMask))
                        continue;

                    tiles.Add((neighbor, tileId));
                    index++;
                    allExterior.Remove(neighbor);
                }
            }
        }

        grid.SetTiles(tiles);
        index = 0;

        foreach (var tile in tiles)
        {
            var gridPos = grid.GridTileToLocal(tile.Item1);

            foreach (var ent in gen.Entities)
            {
                _entManager.SpawnEntity(ent, gridPos);
                index++;
            }

            if (index % 20 == 0)
            {
                await SuspendIfOutOfTime();
            }
        }
    }

    /*
     * You may be wondering why these are different.
     * It's because for internals we want to force it as it looks nicer and not leave it up to chance.
     */

    // TODO: Can probably combine these a bit, their differences are in really annoying to pull out spots.

    private async Task PostGen(InternalWindowPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid,
        Random random)
    {
        // Iterate every room and check if there's a gap beyond it that leads to another room within N tiles
        // If so then consider windows
        var minDistance = 4;
        var maxDistance = 6;

        foreach (var room in dungeon.Rooms)
        {
            var validTiles = new List<Vector2i>();

            for (var i = 0; i < 4; i++)
            {
                var dir = (DirectionFlag) Math.Pow(2, i);
                var dirVec = dir.AsDir().ToIntVec();

                foreach (var tile in room.Tiles)
                {
                    var tileAngle = ((Vector2) tile + grid.TileSize / 2f - room.Center).ToAngle();
                    var roundedAngle = Math.Round(tileAngle.Theta / (Math.PI / 2)) * (Math.PI / 2);

                    var tileVec = (Vector2i) new Angle(roundedAngle).ToVec().Rounded();

                    if (!tileVec.Equals(dirVec))
                        continue;

                    var valid = false;

                    for (var j = 1; j < maxDistance; j++)
                    {
                        var edgeNeighbor = tile + dirVec * j;

                        if (dungeon.RoomTiles.Contains(edgeNeighbor))
                        {
                            if (j < minDistance)
                            {
                                valid = false;
                            }
                            else
                            {
                                valid = true;
                            }

                            break;
                        }
                    }

                    if (!valid)
                        continue;

                    var windowTile = tile + dirVec;

                    if (!_anchorable.TileFree(grid, windowTile, CollisionLayer, CollisionMask))
                        continue;

                    validTiles.Add(windowTile);
                }

                validTiles.Sort((x, y) => ((Vector2) x + grid.TileSize / 2f - room.Center).LengthSquared.CompareTo(((Vector2) y + grid.TileSize / 2f - room.Center).LengthSquared));

                for (var j = 0; j < Math.Min(validTiles.Count, 3); j++)
                {
                    var tile = validTiles[j];
                    var gridPos = grid.GridTileToLocal(tile);
                    grid.SetTile(tile, new Tile(_tileDefManager[gen.Tile].TileId));

                    foreach (var ent in gen.Entities)
                    {
                        _entManager.SpawnEntity(ent, gridPos);
                    }
                }

                if (validTiles.Count > 0)
                {
                    await SuspendIfOutOfTime();
                    ValidateResume();
                }

                validTiles.Clear();
            }
        }
    }

    /// <summary>
    /// Simply places tiles / entities on the entrances to rooms.
    /// </summary>
    private async Task PostGen(RoomEntrancePostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid,
        Random random)
    {
        var setTiles = new List<(Vector2i, Tile)>();
        var tile = new Tile(_tileDefManager[gen.Tile].TileId);

        foreach (var room in dungeon.Rooms)
        {
            foreach (var entrance in room.Entrances)
            {
                setTiles.Add((entrance, tile));
            }
        }

        grid.SetTiles(setTiles);

        foreach (var room in dungeon.Rooms)
        {
            foreach (var entrance in room.Entrances)
            {
                foreach (var ent in gen.Entities)
                {
                    _entManager.SpawnEntity(ent, grid.GridTileToLocal(entrance));
                }
            }
        }
    }

    /// <summary>
    /// Generates corridor connections between entrances to all the rooms.
    /// </summary>
    private async Task PostGen(CorridorPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid, Random random)
    {
        var entrances = new List<Vector2i>(dungeon.Rooms.Count);

        // Grab entrances
        foreach (var room in dungeon.Rooms)
        {
            entrances.AddRange(room.Entrances);
        }

        // Generate connections between all rooms.
        var connections = new Dictionary<Vector2i, List<(Vector2i Tile, float Distance)>>(entrances.Count);

        foreach (var entrance in entrances)
        {
            var edgeConns = new List<(Vector2i Tile, float Distance)>(entrances.Count - 1);

            foreach (var other in entrances)
            {
                if (entrance == other)
                    continue;

                edgeConns.Add((other, (other - entrance).Length));
            }

            // Sort these as they will be iterated many times.
            edgeConns.Sort((x, y) => x.Distance.CompareTo(y.Distance));
            connections.Add(entrance, edgeConns);
        }

        // Pathfind between them, lower weight for nodes we've already generated corridors for.

        // MST
        // Use Prim's algo
        // 0. Pick random vert as seed
        // 1. Of all the tree edges (i.e. for all verts we've added already) pick the lowest weight one and add it to the tree
        // 2. Repeat 1. until all vertices in the tree.
        var seedIndex = random.Next(entrances.Count);
        var remaining = new ValueList<Vector2i>(entrances);
        remaining.RemoveAt(seedIndex);

        var edges = new List<(Vector2i Start, Vector2i End)>();
        var cheapest = (Vector2i.Zero, Vector2i.Zero);

        var seedEntrance = entrances[seedIndex];
        var forest = new ValueList<Vector2i>(entrances.Count) { seedEntrance };

        while (remaining.Count > 0)
        {
            // Get cheapest edge
            var cheapestDistance = float.MaxValue;
            cheapest = (Vector2i.Zero, Vector2i.Zero);

            foreach (var node in forest)
            {
                foreach (var conn in connections[node])
                {
                    // Existing tile, skip
                    if (forest.Contains(conn.Tile))
                        continue;

                    // Not the cheapest
                    if (cheapestDistance < conn.Distance)
                        continue;

                    cheapestDistance = conn.Distance;
                    cheapest = (node, conn.Tile);
                    // List is pre-sorted so we can just breakout easily.
                    break;
                }
            }

            DebugTools.Assert(cheapestDistance < float.MaxValue);
            // Add to tree
            edges.Add(cheapest);
            forest.Add(cheapest.Item2);
            remaining.Remove(cheapest.Item2);
        }

        // TODO: Add in say 1/3 of edges back in to add some cyclic to it.

        // TODO: Probably just need to BSP it I think as the default room packs are incompatible.

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
                        var neighbor = new Vector2i(tile.X + x, tile.Y + y);

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
                var normal = ((Vector2) entrance + grid.TileSize / 2f - room.Center).ToWorldAngle().GetCardinalDir().ToIntVec();
                deterredTiles.Remove(entrance + normal);
            }
        }

        // Pathfind each entrance
        var corridorTiles = new HashSet<Vector2i>();
        var frontier = new PriorityQueue<Vector2i, float>();
        var cameFrom = new Dictionary<Vector2i, Vector2i>();
        var directions = new Dictionary<Vector2i, Direction>();
        var costSoFar = new Dictionary<Vector2i, float>();
        var pathLimit = gen.PathLimit;

        /*
         *  - Fix entrance gen (fallback to middle bits if no markers specified)
            - Have corridors sometimes overshoot (postgen step maybe)
            - Bump poster frequency I think
            - Re-use the markers from above for entrance gen
            - Corridors just uhh regular walls only corners reinforced

            - After ALL the above working, then make entirely NEW templates.
         */

        foreach (var (start, end) in edges)
        {
            frontier.Clear();
            cameFrom.Clear();
            costSoFar.Clear();
            directions.Clear();
            directions[start] = Direction.Invalid;
            frontier.Enqueue(start, 0f);
            costSoFar[start] = 0f;
            var found = false;
            var count = 0;
            await SuspendIfOutOfTime();

            if (!ValidateResume())
                return;

            while (frontier.Count > 0 && count < pathLimit)
            {
                count++;
                var node = frontier.Dequeue();

                if (node == end)
                {
                    found = true;
                    break;
                }

                var lastDirection = directions[node];

                // Foreach neighbor etc etc
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        // Cardinals only.
                        if (x != 0 && y != 0)
                            continue;

                        var neighbor = new Vector2i(node.X + x, node.Y + y);

                        // FORBIDDEN
                        if (neighbor != end &&
                            (dungeon.RoomTiles.Contains(neighbor) ||
                            dungeon.RoomExteriorTiles.Contains(neighbor)))
                        {
                            continue;
                        }

                        var tileCost = PathfindingSystem.ManhattanDistance(node, neighbor);

                        // Weight towards existing corridors ig
                        if (corridorTiles.Contains(neighbor))
                        {
                            tileCost *= 0.10f;
                        }

                        // If it's next to a dungeon room then avoid it if at all possible
                        if (deterredTiles.Contains(neighbor))
                        {
                            tileCost *= 2f;
                        }

                        var direction = (neighbor - node).GetCardinalDir();
                        directions[neighbor] = direction;

                        // If direction is different then penalise it.
                        if (direction != lastDirection)
                        {
                            tileCost *= 3f;
                        }

                        // f = g + h
                        // gScore is distance to the start node
                        // hScore is distance to the end node
                        var gScore = costSoFar[node] + tileCost;

                        if (costSoFar.TryGetValue(neighbor, out var nextValue) && gScore >= nextValue)
                        {
                            continue;
                        }

                        cameFrom[neighbor] = node;
                        costSoFar[neighbor] = gScore;

                        // Make it greedy so multiply h-score to punish further nodes.
                        // This is necessary as we might have the deterredTiles multiplying towards the end
                        // so just finish it.
                        var hScore = PathfindingSystem.ManhattanDistance(end, neighbor) * (1.0f - 1.0f / 1000.0f);
                        var fScore = gScore + hScore;
                        frontier.Enqueue(neighbor, fScore);
                    }
                }
            }

            // Rebuild path if it's valid.
            if (found)
            {
                var node = end;

                while (true)
                {
                    node = cameFrom[node];

                    // Don't want start or end nodes included.
                    if (node == start)
                        break;

                    corridorTiles.Add(node);
                }
            }
        }

        // Widen the path
        if (expansion >= 1)
        {
            var toAdd = new ValueList<Vector2i>();

            foreach (var node in corridorTiles)
            {
                // Uhhh not sure on the cleanest way to do this but tl;dr we don't want to hug
                // exterior walls and make the path smaller.

                for (var x = -expansion; x <= expansion; x++)
                {
                    for (var y = -expansion; y <= expansion; y++)
                    {
                        var neighbor = new Vector2i(node.X + x, node.Y + y);

                        // Diagonals still matter here.
                        if (dungeon.RoomTiles.Contains(neighbor) ||
                            dungeon.RoomExteriorTiles.Contains(neighbor))
                        {
                            // Try

                            continue;
                        }

                        toAdd.Add(neighbor);
                    }
                }
            }

            foreach (var node in toAdd)
            {
                corridorTiles.Add(node);
            }
        }

        var setTiles = new List<(Vector2i, Tile)>();
        var tileType = _tileDefManager["FloorSteel"];
        var tileVariants = tileType.Variants;

        foreach (var tile in corridorTiles)
        {
            setTiles.Add((tile, new Tile(tileType.TileId, variant: (byte) random.Next(tileVariants))));
        }

        grid.SetTiles(setTiles);
        dungeon.CorridorTiles.UnionWith(corridorTiles);

        var exterior = dungeon.CorridorExteriorTiles;

        // Just ignore entrances or whatever for now.
        foreach (var tile in dungeon.CorridorTiles)
        {
            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    var neighbor = new Vector2i(tile.X + x, tile.Y + y);

                    if (dungeon.CorridorTiles.Contains(neighbor))
                        continue;

                    exterior.Add(neighbor);
                }
            }
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

                        if (!_anchorable.TileFree(grid, neighbor, CollisionLayer, CollisionMask))
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

        foreach (var (room, border) in roomBorders)
        {
            var conns = roomConnections.GetOrNew(room);

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
                    if (!_anchorable.TileFree(grid, node, CollisionLayer, CollisionMask))
                        continue;

                    width--;
                    grid.SetTile(node, tile);

                    if (gen.EdgeEntities != null && nodeDistances.Count - i <= 2)
                    {
                        foreach (var ent in gen.EdgeEntities)
                        {
                            _entManager.SpawnEntity(ent, gridPos);
                        }
                    }
                    else
                    {
                        // Iterate neighbors and check for blockers, if so bulldoze
                        ClearDoor(dungeon, grid, node);

                        foreach (var ent in gen.Entities)
                        {
                            _entManager.SpawnEntity(ent, gridPos);
                        }
                    }

                    if (width == 0)
                        break;
                }

                conns.Add(otherRoom);
                var otherConns = roomConnections.GetOrNew(otherRoom);
                otherConns.Add(room);
                await SuspendIfOutOfTime();
                ValidateResume();
            }
        }
    }

    /// <summary>
    /// Removes any unwanted obstacles around a door tile.
    /// </summary>
    private void ClearDoor(Dungeon dungeon, MapGridComponent grid, Vector2i indices, bool strict = false)
    {
        var flags = strict
            ? LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.StaticSundries
            : LookupFlags.Dynamic | LookupFlags.Static;
        var physicsQuery = _entManager.GetEntityQuery<PhysicsComponent>();

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                if (x != 0 && y != 0)
                    continue;

                var neighbor = new Vector2i(indices.X + x, indices.Y + y);

                if (!dungeon.RoomTiles.Contains(neighbor))
                    continue;

                // Shrink by 0.01 to avoid polygon overlap from neighboring tiles.
                foreach (var ent in _lookup.GetEntitiesIntersecting(_gridUid, new Box2(neighbor * grid.TileSize, (neighbor + 1) * grid.TileSize).Enlarged(-0.1f), flags))
                {
                    if (!physicsQuery.TryGetComponent(ent, out var physics) ||
                        !physics.Hard ||
                        (CollisionMask & physics.CollisionLayer) == 0x0 &&
                        (CollisionLayer & physics.CollisionMask) == 0x0)
                    {
                        continue;
                    }

                    _entManager.DeleteEntity(ent);
                }
            }
        }
    }

    private async Task PostGen(WallMountPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid,
        Random random)
    {
        var tileDef = new Tile(_tileDefManager[gen.Tile].TileId);
        var checkedTiles = new HashSet<Vector2i>();
        var allExterior = new HashSet<Vector2i>(dungeon.CorridorExteriorTiles);
        allExterior.UnionWith(dungeon.RoomExteriorTiles);
        var count = 0;

        foreach (var neighbor in allExterior)
        {
            // Occupado
            if (dungeon.RoomTiles.Contains(neighbor) || checkedTiles.Contains(neighbor) || !_anchorable.TileFree(grid, neighbor, CollisionLayer, CollisionMask))
                continue;

            if (!random.Prob(gen.Prob) || !checkedTiles.Add(neighbor))
                continue;

            grid.SetTile(neighbor, tileDef);
            var gridPos = grid.GridTileToLocal(neighbor);

            foreach (var ent in EntitySpawnCollection.GetSpawns(gen.Spawns, random))
            {
                _entManager.SpawnEntity(ent, gridPos);
            }

            count++;

            if (count % 20 == 0)
            {
                await SuspendIfOutOfTime();
            }
        }
    }
}
