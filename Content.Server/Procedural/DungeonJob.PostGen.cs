using System.Linq;
using System.Threading.Tasks;
using Content.Server.Light.Components;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
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

                        if (!_anchorable.TileFree(grid, neighbor, CollisionLayer, CollisionMask))
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

    private async Task PostGen(ExternalWindowPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid,
        Random random)
    {
        // Iterate every room with N chance to spawn windows on that wall per cardinal dir.
        var chance = 0.25;
        var distance = 10;

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

                    var valid = true;

                    for (var j = 1; j < distance; j++)
                    {
                        var edgeNeighbor = tile + dirVec * j;

                        if (dungeon.RoomTiles.Contains(edgeNeighbor))
                        {
                            valid = false;
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

                if (validTiles.Count == 0 || random.NextDouble() > chance)
                    continue;

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

                foreach (var ent in _lookup.GetEntitiesIntersecting(_gridUid, neighbor, flags))
                {
                    if (!physicsQuery.TryGetComponent(ent, out var physics) ||
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

        foreach (var room in dungeon.Rooms)
        {
            foreach (var tile in room.Tiles)
            {
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        if (x != 0 && y != 0)
                        {
                            continue;
                        }

                        var neighbor = new Vector2i(tile.X + x, tile.Y + y);

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
                    }
                }
            }
        }
    }
}
