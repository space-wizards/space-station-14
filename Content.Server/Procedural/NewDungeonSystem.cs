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
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        _console.RegisterCommand("weh", GetRoomPack, CompletionCallback);
        _prototype.PrototypesReloaded += PrototypeReload;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _prototype.PrototypesReloaded -= PrototypeReload;
    }

    private void PrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.TryGetValue(typeof(DungeonRoomPrototype), out var rooms))
        {
            return;
        }

        foreach (var proto in rooms.Modified.Values)
        {
            var roomProto = (DungeonRoomPrototype) proto;
            var query = AllEntityQuery<DungeonAtlasTemplateComponent>();

            while (query.MoveNext(out var comp))
            {
                var uid = comp.Owner;

                if (!roomProto.AtlasPath.Equals(comp.Path))
                    continue;

                QueueDel(uid);
                return;
            }
        }
    }

    private MapId GetOrCreateTemplate(DungeonRoomPrototype proto)
    {
        var query = AllEntityQuery<DungeonAtlasTemplateComponent>();
        DungeonAtlasTemplateComponent? comp;

        while (query.MoveNext(out comp))
        {
            var uid = comp.Owner;

            // Exists
            if (comp.Path?.Equals(proto.AtlasPath) == true)
                return Transform(uid).MapID;
        }

        var mapId = _mapManager.CreateMap();
        _loader.Load(mapId, proto.AtlasPath.ToString());
        comp = AddComp<DungeonAtlasTemplateComponent>(_mapManager.GetMapEntityId(mapId));
        comp.Path = proto.AtlasPath;
        return mapId;
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
        var random = new Random();
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
        // TODO: Need to split this off into its own gen thing
        // Same with walls.

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
                    conns.Contains(otherRoom))
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

                    var width = 1;

                    for (var i = 0; i < nodeDistances.Count; i++)
                    {
                        width--;
                        grid.SetTile(nodeDistances[i].Node, new Tile(_tileDefManager["FloorSteel"].TileId));
                        Spawn("AirlockGlass", grid.GridTileToLocal(nodeDistances[i].Node));

                        if (width == 0)
                            break;
                    }
                }

                conns.Add(otherRoom);
                var otherConns = roomConnections.GetOrNew(otherRoom);
                otherConns.Add(room);
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

                if (roomDimensions.X == roomDimensions.Y)
                {
                    // Give it a random rotation
                    roomRotation = random.Next(4) * Math.PI / 2;
                }
                else if (random.Next(2) == 1)
                {
                    roomRotation += Math.PI;
                }

                var roomTransform = Matrix3.CreateTransform(roomSize.Center - packCenter, roomRotation);

                Matrix3.Multiply(roomTransform, packTransform, out matty);
                Matrix3.Multiply(matty, dungeonTransform, out matty);

                var room = roomProto[random.Next(roomProto.Count)];
                var roomMap = GetOrCreateTemplate(room);
                var templateMapUid = _mapManager.GetMapEntityId(roomMap);
                var templateGrid = Comp<MapGridComponent>(templateMapUid);
                var roomCenter = (room.Offset + room.Size / 2f) * grid.TileSize;

                // Load tiles
                for (var x = 0; x < room.Size.X; x++)
                {
                    for (var y = 0; y < room.Size.Y; y++)
                    {
                        var indices = new Vector2i(x + room.Offset.X, y + room.Offset.Y);
                        var tileRef = templateGrid.GetTileRef(indices);

                        var tilePos = matty.Transform((Vector2) indices + grid.TileSize / 2f - roomCenter);
                        tiles.Add((tilePos.Floored(), tileRef.Tile));
                    }
                }

                grid.SetTiles(tiles);
                tiles.Clear();
                var xformQuery = GetEntityQuery<TransformComponent>();
                var metaQuery = GetEntityQuery<MetaDataComponent>();

                // Load entities
                // TODO: I don't think engine supports full entity copying so we do this piece of shit.
                var bounds = new Box2(room.Offset, room.Offset + room.Size);

                foreach (var templateEnt in _lookup.GetEntitiesIntersecting(templateMapUid, bounds, LookupFlags.Uncontained))
                {
                    var templateXform = xformQuery.GetComponent(templateEnt);
                    var childPos = matty.Transform(templateXform.LocalPosition - roomCenter);

                    var ent = Spawn(metaQuery.GetComponent(templateEnt).EntityPrototype?.ID,
                        new EntityCoordinates(gridUid, childPos));

                    var childXform = xformQuery.GetComponent(ent);
                    var anchored = childXform.Anchored;
                    _transform.SetCoordinates(ent, childXform, new EntityCoordinates(gridUid, childPos));

                    if (anchored && !childXform.Anchored)
                        _transform.AnchorEntity(ent, childXform, grid);
                }

                // Load decals
                if (TryComp<DecalGridComponent>(templateMapUid, out var loadedDecals))
                {
                    EnsureComp<DecalGridComponent>(gridUid);

                    foreach (var (_, decal) in _decals.GetDecalsIntersecting(templateMapUid, bounds, loadedDecals))
                    {
                        // Offset by 0.5 because decals are offset from bot-left corner
                        // So we convert it to center of tile then convert it back again after transform.
                        var position = matty.Transform(decal.Coordinates + 0.5f - roomCenter);
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
        }

        _mapManager.DeleteMap(dummyMap);

        return dungeon;
    }
}
