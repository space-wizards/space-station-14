using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Content.Server.NodeContainer;
using Content.Shared.Doors.Components;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Storage;
using Content.Shared.Tag;
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

    private bool HasWall(MapGridComponent grid, Vector2i tile)
    {
        var anchored = grid.GetAnchoredEntitiesEnumerator(tile);

        while (anchored.MoveNext(out var uid))
        {
            if (_tagQuery.TryGetComponent(uid, out var tagComp) && tagComp.Tags.Contains("Wall"))
                return true;
        }

        return false;
    }

    private async Task PostGen(AutoCablingPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid,
        Random random)
    {
        // There's a lot of ways you could do this.
        // For now we'll just connect every LV cable in the dungeon.
        var cableTiles = new HashSet<Vector2i>();
        var allTiles = new HashSet<Vector2i>(dungeon.CorridorTiles);
        allTiles.UnionWith(dungeon.RoomTiles);
        allTiles.UnionWith(dungeon.RoomExteriorTiles);
        allTiles.UnionWith(dungeon.CorridorExteriorTiles);
        var nodeQuery = _entManager.GetEntityQuery<NodeContainerComponent>();

        // Gather existing nodes
        foreach (var tile in allTiles)
        {
            var anchored = grid.GetAnchoredEntitiesEnumerator(tile);

            while (anchored.MoveNext(out var anc))
            {
                if (!nodeQuery.TryGetComponent(anc, out var nodeContainer) ||
                   !nodeContainer.Nodes.ContainsKey("power"))
                {
                    continue;
                }

                cableTiles.Add(tile);
                break;
            }
        }

        // Iterating them all might be expensive.
        await SuspendIfOutOfTime();

        if (!ValidateResume())
            return;

        var startNodes = new List<Vector2i>(cableTiles);
        random.Shuffle(startNodes);
        var start = startNodes[0];
        var remaining = new HashSet<Vector2i>(startNodes);
        var frontier = new PriorityQueue<Vector2i, float>();
        frontier.Enqueue(start, 0f);
        var cameFrom = new Dictionary<Vector2i, Vector2i>();
        var costSoFar = new Dictionary<Vector2i, float>();
        var lastDirection = new Dictionary<Vector2i, Direction>();
        costSoFar[start] = 0f;
        lastDirection[start] = Direction.Invalid;

        while (remaining.Count > 0)
        {
            if (frontier.Count == 0)
            {
                var newStart = remaining.First();
                frontier.Enqueue(newStart, 0f);
                lastDirection[newStart] = Direction.Invalid;
            }

            var node = frontier.Dequeue();

            if (remaining.Remove(node))
            {
                var weh = node;

                while (cameFrom.TryGetValue(weh, out var receiver))
                {
                    cableTiles.Add(weh);
                    weh = receiver;

                    if (weh == start)
                        break;
                }
            }

            if (!grid.TryGetTileRef(node, out var tileRef) || tileRef.Tile.IsEmpty)
            {
                continue;
            }

            for (var i = 0; i < 4; i++)
            {
                var dir = (Direction) (i * 2);

                var neighbor = node + dir.ToIntVec();
                var tileCost = 1f;

                // Prefer straight lines.
                if (lastDirection[node] != dir)
                {
                    tileCost *= 1.1f;
                }

                if (cableTiles.Contains(neighbor))
                {
                    tileCost *= 0.1f;
                }

                // Prefer tiles without walls on them
                if (HasWall(grid, neighbor))
                {
                    tileCost *= 20f;
                }

                var gScore = costSoFar[node] + tileCost;

                if (costSoFar.TryGetValue(neighbor, out var nextValue) && gScore >= nextValue)
                {
                    continue;
                }

                cameFrom[neighbor] = node;
                costSoFar[neighbor] = gScore;
                lastDirection[neighbor] = dir;
                frontier.Enqueue(neighbor, gScore);
            }
        }

        foreach (var tile in cableTiles)
        {
            var anchored = grid.GetAnchoredEntitiesEnumerator(tile);
            var found = false;

            while (anchored.MoveNext(out var anc))
            {
                if (!nodeQuery.TryGetComponent(anc, out var nodeContainer) ||
                    !nodeContainer.Nodes.ContainsKey("power"))
                {
                    continue;
                }

                found = true;
                break;
            }

            if (found)
                continue;

            _entManager.SpawnEntity("CableApcExtension", _grid.GridTileToLocal(tile));
        }
    }

    private async Task PostGen(BoundaryWallPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid, Random random)
    {
        var tileDef = _tileDefManager[gen.Tile];
        var tiles = new List<(Vector2i Index, Tile Tile)>(dungeon.RoomExteriorTiles.Count);

        // Spawn wall outline
        // - Tiles first
        foreach (var neighbor in dungeon.RoomExteriorTiles)
        {
            if (dungeon.RoomTiles.Contains(neighbor))
                continue;

            if (!_anchorable.TileFree(grid, neighbor, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                continue;

            tiles.Add((neighbor, _tileDefManager.GetVariantTile(tileDef, random)));
        }

        foreach (var index in dungeon.CorridorExteriorTiles)
        {
            if (dungeon.RoomTiles.Contains(index))
                continue;

            if (!_anchorable.TileFree(grid, index, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                continue;

            tiles.Add((index, _tileDefManager.GetVariantTile(tileDef, random)));
        }

        grid.SetTiles(tiles);

        // Double iteration coz we bulk set tiles for speed.
        for (var i = 0; i < tiles.Count; i++)
        {
            var index = tiles[i];
            if (!_anchorable.TileFree(grid, index.Index, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
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

                        if (dungeon.RoomTiles.Contains(neighbor) || dungeon.CorridorTiles.Contains(neighbor))
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

            if (i % 20 == 0)
            {
                await SuspendIfOutOfTime();

                if (!ValidateResume())
                    return;
            }
        }
    }

    private async Task PostGen(CornerClutterPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid,
        Random random)
    {
        var physicsQuery = _entManager.GetEntityQuery<PhysicsComponent>();
        var tagQuery = _entManager.GetEntityQuery<TagComponent>();

        foreach (var tile in dungeon.CorridorTiles)
        {
            var enumerator = _grid.GetAnchoredEntitiesEnumerator(tile);
            var blocked = false;

            while (enumerator.MoveNext(out var ent))
            {
                // TODO: TileFree
                if (!physicsQuery.TryGetComponent(ent, out var physics) ||
                    !physics.CanCollide ||
                    !physics.Hard)
                {
                    continue;
                }

                blocked = true;
                break;
            }

            if (blocked)
                continue;

            // If at least 2 adjacent tiles are blocked consider it a corner
            for (var i = 0; i < 4; i++)
            {
                var dir = (Direction) (i * 2);
                blocked = HasWall(grid, tile + dir.ToIntVec());

                if (!blocked)
                    continue;

                var nextDir = (Direction) ((i + 1) * 2 % 8);
                blocked = HasWall(grid, tile + nextDir.ToIntVec());

                if (!blocked)
                    continue;

                if (random.Prob(gen.Chance))
                {
                    var coords = _grid.GridTileToLocal(tile);
                    var protos = EntitySpawnCollection.GetSpawns(gen.Contents, random);
                    _entManager.SpawnEntities(coords, protos);
                }

                break;
            }
        }
    }

    private async Task PostGen(CorridorDecalSkirtingPostGen decks, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid, Random random)
    {
        var directions = new ValueList<DirectionFlag>(4);
        var pocketDirections = new ValueList<Direction>(4);
        var doorQuery = _entManager.GetEntityQuery<DoorComponent>();
        var physicsQuery = _entManager.GetEntityQuery<PhysicsComponent>();
        var offset = -_grid.TileSizeHalfVector;
        var color = decks.Color;

        foreach (var tile in dungeon.CorridorTiles)
        {
            DebugTools.Assert(!dungeon.RoomTiles.Contains(tile));
            directions.Clear();

            // Do cardinals 1 step
            // Do corners the other step
            for (var i = 0; i < 4; i++)
            {
                var dir = (DirectionFlag) Math.Pow(2, i);
                var neighbor = tile + dir.AsDir().ToIntVec();

                var anc = _grid.GetAnchoredEntitiesEnumerator(neighbor);

                while (anc.MoveNext(out var ent))
                {
                    if (!physicsQuery.TryGetComponent(ent, out var physics) ||
                        !physics.CanCollide ||
                        !physics.Hard ||
                        doorQuery.HasComponent(ent.Value))
                    {
                        continue;
                    }

                    directions.Add(dir);
                    break;
                }
            }

            // Pockets
            if (directions.Count == 0)
            {
                pocketDirections.Clear();

                for (var i = 1; i < 5; i++)
                {
                    var dir = (Direction) (i * 2 - 1);
                    var neighbor = tile + dir.ToIntVec();

                    var anc = _grid.GetAnchoredEntitiesEnumerator(neighbor);

                    while (anc.MoveNext(out var ent))
                    {
                        if (!physicsQuery.TryGetComponent(ent, out var physics) ||
                            !physics.CanCollide ||
                            !physics.Hard ||
                            doorQuery.HasComponent(ent.Value))
                        {
                            continue;
                        }

                        pocketDirections.Add(dir);
                        break;
                    }
                }

                if (pocketDirections.Count == 1)
                {
                    if (decks.PocketDecals.TryGetValue(pocketDirections[0], out var cDir))
                    {
                        // Decals not being centered biting my ass again
                        var gridPos = _grid.GridTileToLocal(tile).Offset(offset);
                        _decals.TryAddDecal(cDir, gridPos, out _, color: color);
                    }
                }

                continue;
            }

            if (directions.Count == 1)
            {
                if (decks.CardinalDecals.TryGetValue(directions[0], out var cDir))
                {
                    // Decals not being centered biting my ass again
                    var gridPos = _grid.GridTileToLocal(tile).Offset(offset);
                    _decals.TryAddDecal(cDir, gridPos, out _, color: color);
                }

                continue;
            }

            // Corners
            if (directions.Count == 2)
            {
                // Auehghegueugegegeheh help me
                var dirFlag = directions[0] | directions[1];

                if (decks.CornerDecals.TryGetValue(dirFlag, out var cDir))
                {
                    var gridPos = _grid.GridTileToLocal(tile).Offset(offset);
                    _decals.TryAddDecal(cDir, gridPos, out _, color: color);
                }
            }
        }
    }

    private async Task PostGen(DungeonEntrancePostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid, Random random)
    {
        var rooms = new List<DungeonRoom>(dungeon.Rooms);
        var roomTiles = new List<Vector2i>();
        var tileDef = _tileDefManager[gen.Tile];

        for (var i = 0; i < gen.Count; i++)
        {
            var roomIndex = random.Next(rooms.Count);
            var room = rooms[roomIndex];

            // Move out 3 tiles in a direction away from center of the room
            // If none of those intersect another tile it's probably external
            // TODO: Maybe need to take top half of furthest rooms in case there's interior exits?
            roomTiles.AddRange(room.Exterior);
            random.Shuffle(roomTiles);

            foreach (var tile in roomTiles)
            {
                var isValid = false;

                // Check if one side is dungeon and the other side is nothing.
                for (var j = 0; j < 4; j++)
                {
                    var dir = (Direction) (j * 2);
                    var oppositeDir = dir.GetOpposite();
                    var dirVec = tile + dir.ToIntVec();
                    var oppositeDirVec = tile + oppositeDir.ToIntVec();

                    if (!dungeon.RoomTiles.Contains(dirVec))
                    {
                        continue;
                    }

                    if (dungeon.RoomTiles.Contains(oppositeDirVec) ||
                        dungeon.RoomExteriorTiles.Contains(oppositeDirVec) ||
                        dungeon.CorridorExteriorTiles.Contains(oppositeDirVec) ||
                        dungeon.CorridorTiles.Contains(oppositeDirVec))
                    {
                        continue;
                    }

                    // Check if exterior spot free.
                    if (!_anchorable.TileFree(_grid, tile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                    {
                        continue;
                    }

                    // Check if interior spot free (no guarantees on exterior but ClearDoor should handle it)
                    if (!_anchorable.TileFree(_grid, dirVec, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                    {
                        continue;
                    }

                    // Valid pick!
                    isValid = true;

                    // Entrance wew
                    grid.SetTile(tile, _tileDefManager.GetVariantTile(tileDef, random));
                    ClearDoor(dungeon, grid, tile);
                    var gridCoords = grid.GridTileToLocal(tile);
                    // Need to offset the spawn to avoid spawning in the room.

                    _entManager.SpawnEntities(gridCoords, gen.Entities);

                    // Clear out any biome tiles nearby to avoid blocking it
                    foreach (var nearTile in grid.GetTilesIntersecting(new Circle(gridCoords.Position, 1.5f), false))
                    {
                        if (dungeon.RoomTiles.Contains(nearTile.GridIndices) ||
                            dungeon.RoomExteriorTiles.Contains(nearTile.GridIndices) ||
                            dungeon.CorridorTiles.Contains(nearTile.GridIndices) ||
                            dungeon.CorridorExteriorTiles.Contains(nearTile.GridIndices))
                        {
                            continue;
                        }

                        grid.SetTile(nearTile.GridIndices, _tileDefManager.GetVariantTile(tileDef, random));
                    }

                    break;
                }

                if (isValid)
                    break;
            }

            roomTiles.Clear();
        }
    }

    private async Task PostGen(ExternalWindowPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid,
        Random random)
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
                        !_anchorable.TileFree(grid, neighbor, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
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

                    tiles.Add((neighbor, _tileDefManager.GetVariantTile(tileDef, random)));
                    index++;
                    takenTiles.Add(neighbor);
                }
            }
        }

        grid.SetTiles(tiles);
        index = 0;

        foreach (var tile in tiles)
        {
            var gridPos = grid.GridTileToLocal(tile.Item1);

            index += gen.Entities.Count;
            _entManager.SpawnEntities(gridPos, gen.Entities);

            if (index > 20)
            {
                index -= 20;
                await SuspendIfOutOfTime();

                if (!ValidateResume())
                    return;
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
        var tileDef = _tileDefManager[gen.Tile];

        foreach (var room in dungeon.Rooms)
        {
            var validTiles = new List<Vector2i>();

            for (var i = 0; i < 4; i++)
            {
                var dir = (DirectionFlag) Math.Pow(2, i);
                var dirVec = dir.AsDir().ToIntVec();

                foreach (var tile in room.Tiles)
                {
                    var tileAngle = ((Vector2) tile + grid.TileSizeHalfVector - room.Center).ToAngle();
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

                    if (!_anchorable.TileFree(grid, windowTile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                        continue;

                    validTiles.Add(windowTile);
                }

                validTiles.Sort((x, y) => ((Vector2) x + grid.TileSizeHalfVector - room.Center).LengthSquared().CompareTo((y + grid.TileSizeHalfVector - room.Center).LengthSquared));

                for (var j = 0; j < Math.Min(validTiles.Count, 3); j++)
                {
                    var tile = validTiles[j];
                    var gridPos = grid.GridTileToLocal(tile);
                    grid.SetTile(tile, _tileDefManager.GetVariantTile(tileDef, random));

                    _entManager.SpawnEntities(gridPos, gen.Entities);
                }

                if (validTiles.Count > 0)
                {
                    await SuspendIfOutOfTime();

                    if (!ValidateResume())
                        return;
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
        var tileDef = _tileDefManager[gen.Tile];

        foreach (var room in dungeon.Rooms)
        {
            foreach (var entrance in room.Entrances)
            {
                setTiles.Add((entrance, _tileDefManager.GetVariantTile(tileDef, random)));
            }
        }

        grid.SetTiles(setTiles);

        foreach (var room in dungeon.Rooms)
        {
            foreach (var entrance in room.Entrances)
            {
                _entManager.SpawnEntities(grid.GridTileToLocal(entrance), gen.Entities);
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

        var edges = _dungeon.MinimumSpanningTree(entrances, random);
        await SuspendIfOutOfTime();

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
                var normal = ((Vector2) entrance + grid.TileSizeHalfVector - room.Center).ToWorldAngle().GetCardinalDir().ToIntVec();
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
        var tileDef = _tileDefManager["FloorSteel"];

        foreach (var tile in corridorTiles)
        {
            setTiles.Add((tile, _tileDefManager.GetVariantTile(tileDef, random)));
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

    private async Task PostGen(EntranceFlankPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid,
        Random random)
    {
        var tiles = new List<(Vector2i Index, Tile)>();
        var tileDef = _tileDefManager[gen.Tile];
        var spawnPositions = new ValueList<Vector2i>(dungeon.Rooms.Count);

        foreach (var room in dungeon.Rooms)
        {
            foreach (var entrance in room.Entrances)
            {
                for (var i = 0; i < 8; i++)
                {
                    var dir = (Direction) i;
                    var neighbor = entrance + dir.ToIntVec();

                    if (!dungeon.RoomExteriorTiles.Contains(neighbor))
                        continue;

                    tiles.Add((neighbor, _tileDefManager.GetVariantTile(tileDef, random)));
                    spawnPositions.Add(neighbor);
                }
            }
        }

        grid.SetTiles(tiles);

        foreach (var entrance in spawnPositions)
        {
            _entManager.SpawnEntities(_grid.GridTileToLocal(entrance), gen.Entities);
        }
    }

    private async Task PostGen(JunctionPostGen gen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid,
        Random random)
    {
        var tileDef = _tileDefManager[gen.Tile];

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
                        if (!HasWall(grid, neighbor))
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
                        grid.SetTile(weh, _tileDefManager.GetVariantTile(tileDef, random));

                        var coords = grid.GridTileToLocal(weh);
                        _entManager.SpawnEntities(coords, gen.Entities);
                    }

                    break;
                }

                if (isValid)
                {
                    await SuspendIfOutOfTime();

                    if (!ValidateResume())
                        return;
                }

                break;
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

                        if (!_anchorable.TileFree(grid, neighbor, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
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
        var tileDef = _tileDefManager[gen.Tile];

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
                    center += (Vector2) node + grid.TileSizeHalfVector;
                }

                center /= flipp.Count;
                // Weight airlocks towards center more.
                var nodeDistances = new List<(Vector2i Node, float Distance)>(flipp.Count);

                foreach (var node in flipp)
                {
                    nodeDistances.Add((node, ((Vector2) node + grid.TileSizeHalfVector - center).LengthSquared()));
                }

                nodeDistances.Sort((x, y) => x.Distance.CompareTo(y.Distance));

                var width = gen.Count;

                for (var i = 0; i < nodeDistances.Count; i++)
                {
                    var node = nodeDistances[i].Node;
                    var gridPos = grid.GridTileToLocal(node);
                    if (!_anchorable.TileFree(grid, node, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                        continue;

                    width--;
                    grid.SetTile(node, _tileDefManager.GetVariantTile(tileDef, random));

                    if (gen.EdgeEntities != null && nodeDistances.Count - i <= 2)
                    {
                        _entManager.SpawnEntities(gridPos, gen.EdgeEntities);
                    }
                    else
                    {
                        // Iterate neighbors and check for blockers, if so bulldoze
                        ClearDoor(dungeon, grid, node);

                        _entManager.SpawnEntities(gridPos, gen.Entities);
                    }

                    if (width == 0)
                        break;
                }

                conns.Add(otherRoom);
                var otherConns = roomConnections.GetOrNew(otherRoom);
                otherConns.Add(room);
                await SuspendIfOutOfTime();

                if (!ValidateResume())
                    return;
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
                        (DungeonSystem.CollisionMask & physics.CollisionLayer) == 0x0 &&
                        (DungeonSystem.CollisionLayer & physics.CollisionMask) == 0x0)
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
        var tileDef = _tileDefManager[gen.Tile];
        var checkedTiles = new HashSet<Vector2i>();
        var allExterior = new HashSet<Vector2i>(dungeon.CorridorExteriorTiles);
        allExterior.UnionWith(dungeon.RoomExteriorTiles);
        var count = 0;

        foreach (var neighbor in allExterior)
        {
            // Occupado
            if (dungeon.RoomTiles.Contains(neighbor) || checkedTiles.Contains(neighbor) || !_anchorable.TileFree(grid, neighbor, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                continue;

            if (!random.Prob(gen.Prob) || !checkedTiles.Add(neighbor))
                continue;

            grid.SetTile(neighbor, _tileDefManager.GetVariantTile(tileDef, random));
            var gridPos = grid.GridTileToLocal(neighbor);
            var protoNames = EntitySpawnCollection.GetSpawns(gen.Spawns, random);

            _entManager.SpawnEntities(gridPos, protoNames);
            count += protoNames.Count;

            if (count > 20)
            {
                count -= 20;
                await SuspendIfOutOfTime();

                if (!ValidateResume())
                    return;
            }
        }
    }
}
