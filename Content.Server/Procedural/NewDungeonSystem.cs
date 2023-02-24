using System.Linq;
using Content.Server.Decals;
using Content.Shared.Decals;
using Content.Shared.Procedural;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Collections;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

public sealed class NewDungeonSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        _console.RegisterCommand("weh", GetRoomPack, CompletionCallback);
    }

    private CompletionResult CompletionCallback(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), "Map Id");
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<DungeonPresetPrototype>(proto: _prototype), $"Dungeon preset");
        }

        return CompletionResult.Empty;
    }

    private void GetRoomPack(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 2)
        {
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
        {
            return;
        }

        var mapId = new MapId(mapInt);
        var mapUid = _mapManager.GetMapEntityId(mapId);

        if (!TryComp<MapGridComponent>(mapUid, out var mapGrid))
        {
            return;
        }

        if (!_prototype.TryIndex<DungeonPresetPrototype>(args[1], out var dungeon))
        {
            return;
        }

        var random = new Random();

        GetRoomPackDungeon(dungeon, mapUid, mapGrid, random.Next());
    }

    public Dungeon GetRoomPackDungeon(DungeonPresetPrototype gen, EntityUid gridUid, MapGridComponent grid, int seed)
    {
        // Naughty seeds
        // 952532317 (no hook and top bit???)
        // 200682996 (left is so fucking far)

        Logger.Info($"Generating dungeon for seed {seed}");
        var dungeonTransform = Matrix3.CreateTranslation(Vector2.Zero);
        var random = new Random(seed);
        // TODO: API for this
        var roomPackProtos = new Dictionary<Vector2i, List<DungeonRoomPackPrototype>>();
        var externalNodes = new Dictionary<DungeonRoomPackPrototype, HashSet<Vector2i>>();

        foreach (var pack in _prototype.EnumeratePrototypes<DungeonRoomPackPrototype>())
        {
            var size = pack.Size;
            var sizePacks = roomPackProtos.GetOrNew(size);
            sizePacks.Add(pack);

            // Determine external connections; these are only valid when adjacent to a room node.
            // We use this later to determine which room packs connect to each other
            var nodes = new HashSet<Vector2i>();
            externalNodes.Add(pack, nodes);

            foreach (var room in pack.Rooms)
            {
                var rator = new Box2iEdgeEnumerator(room, false);

                while (rator.MoveNext(out var index))
                {
                    nodes.Add(index);
                }
            }
        }

        var roomProtos = new Dictionary<Vector2i, List<DungeonRoomPrototype>>();

        foreach (var proto in _prototype.EnumeratePrototypes<DungeonRoomPrototype>())
        {
            var size = proto.Size;
            var sizeRooms = roomProtos.GetOrNew(size);
            sizeRooms.Add(proto);
        }

        // First we gather all of the edges for each roompack in the preset
        // This allows us to determine which ones should connect from being adjacent
        var edges = new HashSet<Vector2i>[gen.RoomPacks.Count];

        for (var i = 0; i < gen.RoomPacks.Count; i++)
        {
            var pack = gen.RoomPacks[i];
            var nodes = new HashSet<Vector2i>(pack.Width + 2 + pack.Height);

            var rator = new Box2iEdgeEnumerator(pack, false);

            while (rator.MoveNext(out var index))
            {
                nodes.Add(index);
            }

            edges[i] = nodes;
        }

        // Build up edge groups between each pack.
        var connections = new Dictionary<int, Dictionary<int, HashSet<Vector2i>>>();

        for (var i = 0; i < edges.Length; i++)
        {
            var nodes = edges[i];
            var nodeConnections = connections.GetOrNew(i);

            for (var j = i + 1; j < edges.Length; j++)
            {
                var otherNodes = edges[j];
                var intersect = new HashSet<Vector2i>(nodes);

                intersect.IntersectWith(otherNodes);

                if (intersect.Count == 0)
                    continue;

                nodeConnections[j] = intersect;
                var otherNodeConnections = connections.GetOrNew(j);
                otherNodeConnections[i] = intersect;
            }
        }

        var tiles = new List<(Vector2i, Tile)>();
        var dungeon = new Dungeon();
        var dummyMap = _mapManager.CreateMap();
        var availablePacks = new List<DungeonRoomPackPrototype>();
        var chosenPacks = new DungeonRoomPackPrototype?[gen.RoomPacks.Count];
        var packTransforms = new Matrix3[gen.RoomPacks.Count];
        // TODO: Shouldn't need this
        var packRotations = new Angle[gen.RoomPacks.Count];
        var rotatedPackNodes = new HashSet<Vector2i>[gen.RoomPacks.Count];

        // Actually pick the room packs and rooms
        for (var i = 0; i < gen.RoomPacks.Count; i++)
        {
            var bounds = gen.RoomPacks[i];
            var dimensions = new Vector2i(bounds.Width, bounds.Height);

            // Try every pack rotation
            if (roomPackProtos.TryGetValue(dimensions, out var roomPacks))
            {
                availablePacks.AddRange(roomPacks);
            }

            // Try rotated versions if there are any.
            if (dimensions.X != dimensions.Y)
            {
                var rotatedDimensions = new Vector2i(dimensions.Y, dimensions.X);

                if (roomPackProtos.TryGetValue(rotatedDimensions, out roomPacks))
                {
                    availablePacks.AddRange(roomPacks);
                }
            }

            // Iterate every pack
            // To be valid it needs its edge nodes to overlap with every edge group
            var external = connections[i];

            random.Shuffle(availablePacks);
            Matrix3 packTransform = default!;
            var found = false;
            DungeonRoomPackPrototype pack = default!;

            foreach (var aPack in availablePacks)
            {
                var aExternal = externalNodes[aPack];

                for (var j = 0; j < 4; j++)
                {
                    var dir = (DirectionFlag) Math.Pow(2, j);
                    Vector2i aPackDimensions;

                    if ((dir & (DirectionFlag.East | DirectionFlag.West)) != 0x0)
                    {
                        aPackDimensions = new Vector2i(aPack.Size.Y, aPack.Size.X);
                    }
                    else
                    {
                        aPackDimensions = aPack.Size;
                    }

                    // Rotation doesn't match.
                    if (aPackDimensions != bounds.Size)
                        continue;

                    found = true;
                    var rotatedNodes = new HashSet<Vector2i>(aExternal.Count);
                    var aRotation = dir.AsDir().ToAngle();

                    // Get the external nodes in terms of the dungeon layout
                    // (i.e. rotated if necessary + translated to the room position)
                    foreach (var node in aExternal)
                    {
                        // Get the node in pack terms (offset from center), then rotate it
                        // Afterwards we offset it by where the pack is supposed to be in world terms.
                        var rotated = aRotation.RotateVec((Vector2) node + grid.TileSize / 2f - aPack.Size / 2f);
                        rotatedNodes.Add((rotated + bounds.Center).Floored());
                    }

                    foreach (var group in external.Values)
                    {
                        if (rotatedNodes.Overlaps(group))
                            continue;

                        found = false;
                        break;
                    }

                    if (!found)
                    {
                        continue;
                    }

                    // Use this pack
                    packTransform = Matrix3.CreateTransform(bounds.Center, aRotation);
                    packRotations[i] = aRotation;
                    rotatedPackNodes[i] = rotatedNodes;
                    pack = aPack;
                    break;
                }

                if (found)
                    break;
            }

            availablePacks.Clear();

            // Oop
            if (!found)
            {
                continue;
            }

            // If we're not the first pack then connect to our edges.
            chosenPacks[i] = pack;
            packTransforms[i] = packTransform;
        }

        // Grab all of the room bounds
        // Then, work out connections between them
        // TODO: Could use arrays given we do know room count up front
        var rooms = new ValueList<Box2i>(chosenPacks.Length);
        var roomBorders = new Dictionary<Box2i, HashSet<Vector2i>>(chosenPacks.Length);

        for (var i = 0; i < chosenPacks.Length; i++)
        {
            var pack = chosenPacks[i];
            var transform = packTransforms[i];

            foreach (var room in pack!.Rooms)
            {
                // Rooms are at 0,0, need them offset from center
                var offRoom = ((Box2) room).Translated(-pack.Size / 2f);

                var dRoom = (Box2i) transform.TransformBox(offRoom);
                rooms.Add(dRoom);
                DebugTools.Assert(dRoom.Size.X * dRoom.Size.Y == room.Size.X * room.Size.Y);
                var roomEdges = new HashSet<Vector2i>();
                var rator = new Box2iEdgeEnumerator(dRoom, false);

                while (rator.MoveNext(out var edge))
                {
                    roomEdges.Add(edge);
                }

                roomBorders.Add(dRoom, roomEdges);
            }
        }

        // Do pathfind from first room to work out graph.
        // TODO: Optional loops
        var roomConnections = new Dictionary<Box2i, List<Box2i>>();
        var frontier = new Queue<Box2i>();
        frontier.Enqueue(rooms[0]);

        while (frontier.TryDequeue(out var room))
        {
            var conns = roomConnections.GetOrNew(room);
            var border = roomBorders[room];

            foreach (var (otherRoom, otherBorders) in roomBorders)
            {
                if (room.Equals(otherRoom) ||
                    roomConnections.ContainsKey(otherRoom))
                {
                    continue;
                }

                var flipp = new HashSet<Vector2i>(border);
                flipp.IntersectWith(otherBorders);

                if (flipp.Count == 0)
                    continue;

                // Spawn the edge airlocks
                // Weight towards center of the group but not always.

                // If there's 3 overlaps just do a 3x1
                if (flipp.Count == 3)
                {
                    foreach (var node in flipp)
                    {
                        grid.SetTile(node, new Tile(_tileDefManager["FloorSteel"].TileId));
                        Spawn("AirlockGlass", grid.GridTileToLocal(node));
                    }
                }
                else
                {
                    // Pick a random one weighted towards the center
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

                    var weightMax = nodeDistances.Max(o => o.Distance);
                    var weightSum = nodeDistances.Sum(o => (weightMax - o.Distance));
                    var value = random.NextFloat() * weightSum;
                    var width = 1;

                    for (var i = 0; i < nodeDistances.Count; i++)
                    {
                        value -= (weightMax - nodeDistances[i].Distance);

                        if (value < 0f)
                        {
                            width--;
                            grid.SetTile(nodeDistances[i].Node, new Tile(_tileDefManager["FloorSteel"].TileId));
                            Spawn("AirlockGlass", grid.GridTileToLocal(nodeDistances[i].Node));

                            if (width == 0)
                                break;
                        }
                    }
                }

                conns.Add(otherRoom);
                frontier.Enqueue(otherRoom);
            }
        }

        // Then for overlaps choose either 1x1 / 3x1
        // Pick a random tile for it and then expand outwards as relevant (weighted towards middle?)

        for (var i = 0; i < chosenPacks.Length; i++)
        {
            var pack = chosenPacks[i]!;
            var packTransform = packTransforms[i];
            var packRotation = packRotations[i];

            // Actual spawn cud here.
            // Pickout the room pack template to get the room dimensions we need.
            // TODO: Need to be able to load entities on top of other entities but das a lot of effo
            var packCenter = (Vector2) pack.Size / 2;

            foreach (var roomSize in pack.Rooms)
            {
                var roomDimensions = new Vector2i(roomSize.Width, roomSize.Height);
                Angle roomRotation = Angle.Zero;
                Matrix3 matty;

                if (!roomProtos.TryGetValue(roomDimensions, out var roomProto))
                {
                    roomDimensions = new Vector2i(roomDimensions.Y, roomDimensions.X);

                    if (!roomProtos.TryGetValue(roomDimensions, out roomProto))
                    {
                        Matrix3.Multiply(packTransform, dungeonTransform, out matty);

                        for (var x = roomSize.Left; x < roomSize.Right; x++)
                        {
                            for (var y = roomSize.Bottom; y < roomSize.Top; y++)
                            {
                                var index = (Vector2i) matty.Transform(new Vector2(x, y) + grid.TileSize / 2f - packCenter);
                                tiles.Add((index, new Tile(_tileDefManager["FloorPlanetGrass"].TileId)));
                            }
                        }

                        grid.SetTiles(tiles);
                        tiles.Clear();
                        Logger.Error($"Unable to find room variant for {roomDimensions}, leaving empty.");
                        continue;
                    }

                    roomRotation = new Angle(Math.PI / 2);
                    Logger.Debug($"Using rotated variant for room");
                }

                var roomTransform = Matrix3.CreateTransform(roomSize.Center - packCenter, roomRotation);

                Matrix3.Multiply(roomTransform, packTransform, out matty);
                Matrix3.Multiply(matty, dungeonTransform, out matty);

                var room = roomProto[random.Next(roomProto.Count)];

                // Load contents onto a dummy map and copy across as there's no current way
                // to overwrite tile contents via maploader.
                if (!_loader.TryLoad(dummyMap, room.Path.ToString(), out var loadedEnts) ||
                    loadedEnts.Count != 1 ||
                    !TryComp<MapGridComponent>(loadedEnts[0], out var loadedGrid))
                {
                    // A
                    if (loadedEnts != null)
                    {
                        foreach (var ent in loadedEnts)
                        {
                            Del(ent);
                        }
                    }

                    continue;
                }

                var loadedEnumerator = loadedGrid.GetAllTilesEnumerator();

                // Load tiles
                while (loadedEnumerator.MoveNext(out var tile))
                {
                    var tilePos = matty.Transform((Vector2) tile.Value.GridIndices + grid.TileSize / 2f - roomDimensions / 2f);
                    tiles.Add((tilePos.Floored(), tile.Value.Tile));
                }

                grid.SetTiles(tiles);
                tiles.Clear();
                var xformQuery = GetEntityQuery<TransformComponent>();

                // Load entities
                foreach (var ent in Transform(loadedEnts[0]).ChildEntities)
                {
                    var childXform = xformQuery.GetComponent(ent);
                    var childPos = matty.Transform(childXform.LocalPosition - roomDimensions / 2f);
                    var anchored = childXform.Anchored;
                    _transform.SetCoordinates(childXform, new EntityCoordinates(gridUid, childPos));

                    if (anchored)
                        _transform.AnchorEntity(childXform, grid);
                }

                // Load decals
                if (TryComp<DecalGridComponent>(loadedEnts[0], out var loadedDecals))
                {
                    EnsureComp<DecalGridComponent>(gridUid);

                    foreach (var chunk in loadedDecals.ChunkCollection.ChunkCollection.Values)
                    {
                        foreach (var decal in chunk.Decals.Values)
                        {
                            // Offset by 0.5 because decals are offset from bot-left corner
                            // So we convert it to center of tile then convert it back again after transform.
                            var position = matty.Transform(decal.Coordinates + 0.5f - roomDimensions / 2f);
                            position -= 0.5f;
                            _decals.TryAddDecal(
                                decal.Id,
                                new EntityCoordinates(gridUid, position),
                                out _,
                                decal.Color,
                                decal.Angle + roomRotation + packRotation,
                                decal.ZIndex,
                                decal.Cleanable);
                        }
                    }
                }

                // Just in case cleanup the old grid.
                Del(loadedEnts[0]);
                Matrix3.Multiply(packTransform, dungeonTransform, out matty);

                // Spawn wall outline
                // - Tiles first
                var rator = new Box2iEdgeEnumerator(roomSize, true);

                while (rator.MoveNext(out var index))
                {
                    index = matty.Transform((Vector2) index + grid.TileSize / 2f - packCenter).Floored();
                    var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(index);

                    // Occupied tile.
                    if (anchoredEnts.MoveNext(out _))
                        continue;

                    tiles.Add((index, new Tile(_tileDefManager["FloorSteel"].TileId)));
                }

                grid.SetTiles(tiles);
                tiles.Clear();

                // Double iteration coz we bulk set tiles for speed.
                rator = new Box2iEdgeEnumerator(roomSize, true);

                while (rator.MoveNext(out var index))
                {
                    index = matty.Transform((Vector2) index + grid.TileSize / 2f - packCenter).Floored();
                    var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(index);

                    // Occupied tile.
                    if (anchoredEnts.MoveNext(out _))
                        continue;

                    Spawn("WallSolid", grid.GridTileToLocal(index));
                }
            }

            // TODO: Iterate rooms and spawn the walls here.
        }

        _mapManager.DeleteMap(dummyMap);

        return dungeon;
    }
}
