using Content.Server.Decals;
using Content.Shared.Decals;
using Content.Shared.Procedural;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
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

        if (!TryComp<MapGridComponent>(_mapManager.GetMapEntityId(mapId), out var mapGrid))
        {
            return;
        }

        if (!_prototype.TryIndex<DungeonPresetPrototype>(args[1], out var dungeon))
        {
            return;
        }

        var random = new Random();

        GetRoomPackDungeon(dungeon, mapGrid, random.Next());
    }

    public Dungeon GetRoomPackDungeon(DungeonPresetPrototype gen, MapGridComponent grid, int seed)
    {
        var gridUid = grid.Owner;
        var dungeonTransform = Matrix3.CreateTranslation(Vector2.Zero);
        var random = new Random(seed + 256);
        // TODO: API for this
        var roomPackProtos = new Dictionary<Vector2i, List<DungeonRoomPackPrototype>>();
        var externalNodes = new Dictionary<DungeonRoomPackPrototype, HashSet<Vector2i>>();

        foreach (var pack in _prototype.EnumeratePrototypes<DungeonRoomPackPrototype>())
        {
            var size = pack.Size;
            var sizePacks = roomPackProtos.GetOrNew(size);
            sizePacks.Add(pack);

            // Determine external connections
            // We use this later to determine which room packs connect to each other
            var nodes = new HashSet<Vector2i>();
            externalNodes.Add(pack, nodes);

            foreach (var room in pack.Rooms)
            {
                for (var x = room.Left - 1; x <= room.Right; x++)
                {
                    for (var y = room.Bottom - 1; y <= room.Top; y++)
                    {
                        // No interior nodes
                        if (x != -1 && x != pack.Size.X &&
                            y != -1 && y != pack.Size.Y)
                        {
                            continue;
                        }

                        // No corners
                        if (x == -1 && (y == -1 || y == pack.Size.Y) ||
                            x == pack.Size.X && (y == -1 || y == pack.Size.Y))
                        {
                            continue;
                        }

                        nodes.Add(new Vector2i(x, y));
                    }
                }
            }

            // TODO: Determine internal room connections
            // Don't worry about MST, do it later after we have all the chosen packs
            // then work through all room connections and get minimal

            // TODO: Get edge groups for the room pack bounds, then for any actual room packs we need all edges to intersect
            // for every group

            // TODO: Make sure you support rollback in case you can't find a valid roompack for a certain thing.
        }

        var roomProtos = new Dictionary<Vector2i, List<DungeonRoomPrototype>>();

        foreach (var proto in _prototype.EnumeratePrototypes<DungeonRoomPrototype>())
        {
            var size = proto.Size;
            var sizeRooms = roomProtos.GetOrNew(size);
            sizeRooms.Add(proto);
        }

        // First we gather all of the edges for each pack
        // This allows us to determine which ones should connect from being adjacent
        var edges = new HashSet<Vector2i>[gen.RoomPacks.Count];

        for (var i = 0; i < gen.RoomPacks.Count; i++)
        {
            var pack = gen.RoomPacks[i];
            var nodes = new HashSet<Vector2i>(pack.Width + 2 + pack.Height);

            for (var x = pack.Left - 1; x <= pack.Right; x++)
            {
                for (var y = pack.Bottom - 1; y <= pack.Top; y++)
                {
                    // Ignore interior
                    if (x != pack.Left - 1 &&
                        x != pack.Right &&
                        y != pack.Bottom - 1 &&
                        y != pack.Top)
                    {
                        continue;
                    }

                    // If we're in corners then ignore, cardinals only.
                    if (x == pack.Left - 1 && (y == pack.Bottom - 1 || y == pack.Top) ||
                        x == pack.Right && (y == pack.Bottom - 1 || y == pack.Top))
                    {
                        continue;
                    }

                    nodes.Add(new Vector2i(x, y));
                }
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

        foreach (var pack in gen.RoomPacks)
        {
            var dimensions = new Vector2i(pack.Width, pack.Height);
            Matrix3 packTransform;

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

            // TODO: for each dungeonroompackprototype determine external connections automatically
            // TODO: For each dungeonroompackprototype determine internal connections
            // When iterating room spawning (starting on connecting edge) check each connection and see if it's been found yet
            // If it has then we can spawn a room with n-1 available connections

            // Iterate every pack
            foreach (var aPack in availablePacks)
            {

            }

            // Iterate every pack and see what fits
            // Unless it's the first pack then at least one of its connections needs to match.

            // Iterate everything that matches our bounds (or the rotated version) randomly
            // For each one, check connections that

            // Fallback tiles for debug.
            if (!roomPackProtos.TryGetValue(dimensions, out var packs))
            {
                dimensions = new Vector2i(pack.Height, pack.Width);

                if (!roomPackProtos.TryGetValue(dimensions, out packs))
                {
                    Logger.Error($"No room pack found for {dimensions}, using dummy floor.");

                    for (var x = pack.Left; x < pack.Right; x++)
                    {
                        for (var y = pack.Bottom; y < pack.Top; y++)
                        {
                            var index = (Vector2i) dungeonTransform.Transform(new Vector2i(x, y));
                            tiles.Add((index, new Tile(_tileDefManager["FloorSteel"].TileId)));
                        }
                    }

                    grid.SetTiles(tiles);
                    tiles.Clear();
                    continue;
                }

                // To avoid rounding issues.
                packTransform = new Matrix3(0f, -1f, pack.Center.X, 1f, 0f, pack.Center.Y, 0f, 0f, 1f);
                Logger.Debug($"Using rotated variant for rooms pack");
            }
            else
            {
                packTransform = Matrix3.CreateTranslation(pack.Center);
            }

            // Actual spawn cud here.
            // Pickout the room pack template to get the room dimensions we need.
            // TODO: Need to be able to load entities on top of other entities but das a lot of effo
            var gottem = packs[random.Next(packs.Count)];
            var packCenter = (Vector2) gottem.Size / 2;

            foreach (var roomSize in gottem.Rooms)
            {
                var roomDimensions = new Vector2i(roomSize.Width, roomSize.Height);
                var roomCenter = roomDimensions / 2f;
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
                    var tilePos = matty.Transform((Vector2) tile.Value.GridIndices + grid.TileSize / 2f - roomCenter);
                    tiles.Add(((Vector2i) tilePos, tile.Value.Tile));
                }

                grid.SetTiles(tiles);
                tiles.Clear();
                var xformQuery = GetEntityQuery<TransformComponent>();

                // Load entities
                foreach (var ent in Transform(loadedEnts[0]).ChildEntities)
                {
                    var childXform = xformQuery.GetComponent(ent);
                    var childPos = matty.Transform(childXform.LocalPosition - roomCenter);
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
                            var position = matty.Transform(decal.Coordinates + 0.5f - roomCenter);
                            position -= 0.5f;
                            _decals.TryAddDecal(
                                decal.Id,
                                new EntityCoordinates(gridUid, position),
                                out _,
                                decal.Color,
                                decal.Angle,
                                decal.ZIndex,
                                decal.Cleanable);
                        }
                    }
                }

                // Just in case cleanup the old grid.
                Del(loadedEnts[0]);
                // TODO: Spawn doors

                // Spawn wall outline
                // - Tiles first
                for (var x = -1; x <= roomSize.Width; x++)
                {
                    for (var y = -1; y <= roomSize.Height; y++)
                    {
                        if (x != -1 && x != roomSize.Width &&
                            y != -1 && y != roomSize.Height)
                        {
                            continue;
                        }

                        var index = (Vector2i) matty.Transform(new Vector2(x, y) + grid.TileSize / 2f - roomCenter);

                        var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(index);

                        // Occupied tile.
                        if (anchoredEnts.MoveNext(out _))
                            continue;

                        tiles.Add((index, new Tile(_tileDefManager["FloorSteel"].TileId)));
                    }
                }

                grid.SetTiles(tiles);
                tiles.Clear();

                // Double iteration coz we bulk set tiles for speed.
                for (var x = -1; x <= roomSize.Width; x++)
                {
                    for (var y = -1; y <= roomSize.Height; y++)
                    {
                        if (x != -1 && x != roomSize.Width &&
                            y != -1 && y != roomSize.Height)
                        {
                            continue;
                        }

                        var index = (Vector2i) matty.Transform(new Vector2(x, y) + grid.TileSize / 2f - roomCenter);

                        var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(index);

                        // Occupied tile.
                        if (anchoredEnts.MoveNext(out _))
                            continue;

                        Spawn("WallSolid", grid.GridTileToLocal(index));
                    }
                }
            }

            // TODO: Iterate rooms and spawn the walls here.
        }

        _mapManager.DeleteMap(dummyMap);

        return dungeon;
    }
}
