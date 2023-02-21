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
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        _console.RegisterCommand("weh", GetRoomPack);
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

        var random = new System.Random();

        GetRoomPackDungeon(dungeon, mapGrid, random.Next());
    }

    public Dungeon GetRoomPackDungeon(DungeonPresetPrototype gen, MapGridComponent grid, int seed)
    {
        var dungeonTransform = Matrix3.CreateTranslation(Vector2.Zero);
        var random = new Random(seed + 256);
        // TODO: API for this
        var roomPackProtos = new Dictionary<Vector2i, List<DungeonRoomPackPrototype>>();

        foreach (var pack in _prototype.EnumeratePrototypes<DungeonRoomPackPrototype>())
        {
            var size = pack.Size;
            var sizePacks = roomPackProtos.GetOrNew(size);
            sizePacks.Add(pack);
        }

        var roomProtos = new Dictionary<Vector2i, List<DungeonRoomPrototype>>();

        foreach (var proto in _prototype.EnumeratePrototypes<DungeonRoomPrototype>())
        {
            var size = proto.Size;
            var sizeRooms = roomProtos.GetOrNew(size);
            sizeRooms.Add(proto);
        }

        var tiles = new List<(Vector2i, Tile)>();
        var dungeon = new Dungeon();
        var dummyMap = _mapManager.CreateMap();
        var dummyMapUid = _mapManager.GetMapEntityId(dummyMap);

        foreach (var pack in gen.RoomPacks)
        {
            var dimensions = new Vector2i(pack.Width, pack.Height);
            Matrix3 packTransform;

            // Fallback tiles for debug.
            if (!roomPackProtos.TryGetValue(dimensions, out var packs))
            {
                dimensions = new Vector2i(pack.Height, pack.Width);

                if (!roomPackProtos.TryGetValue(dimensions, out packs))
                {
                    Logger.Error($"No room pack found for {dimensions}, using dummy floor.");
                    packTransform = Matrix3.CreateTranslation(pack.Center);

                    for (var x = 0; x < pack.Width; x++)
                    {
                        for (var y = 0; y < pack.Height; y++)
                        {
                            var index = (Vector2i) packTransform.Transform(new Vector2i(x, y));
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

                if (!roomProtos.TryGetValue(roomDimensions, out var roomProto))
                {
                    roomDimensions = new Vector2i(roomDimensions.Y, roomDimensions.X);

                    if (!roomProtos.TryGetValue(roomDimensions, out roomProto))
                    {
                        Logger.Error($"Unable to find room variant for {roomDimensions}, leaving empty.");
                        continue;
                    }

                    roomRotation = new Angle(Math.PI / 2);
                    Logger.Debug($"Using rotated variant for room");
                }

                var roomTransform = Matrix3.CreateTransform(roomSize.Center - packCenter, roomRotation);

                Matrix3.Multiply(roomTransform, packTransform, out var matty);

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

                while (loadedEnumerator.MoveNext(out var tile))
                {
                    var tilePos = matty.Transform((Vector2) tile.Value.GridIndices + grid.TileSize / 2f - roomCenter);
                    tiles.Add(((Vector2i) tilePos, tile.Value.Tile));
                }

                grid.SetTiles(tiles);
                tiles.Clear();
                var xformQuery = GetEntityQuery<TransformComponent>();

                foreach (var ent in Transform(loadedEnts[0]).ChildEntities)
                {
                    Del(ent);
                    /*
                    var childXform = xformQuery.GetComponent(ent);
                    var childPos = transform.Transform(childXform.LocalPosition);
                    var anchored = childXform.Anchored;
                    _transform.SetCoordinates(childXform, new EntityCoordinates(grid.Owner, childPos));

                    if (anchored)
                        _transform.AnchorEntity(childXform, grid);
                        */
                }

                // TODO: Decals

                // Now copy entities
                // foreach (var )

                // TODO: Spawn doors

                // Spawn wall outline
                // - Tiles first
                /*
                for (var x = roomSize.Left - 1; x <= roomSize.Right; x++)
                {
                    for (var y = roomSize.Bottom - 1; y <= roomSize.Top; y++)
                    {
                        if (x != roomSize.Left - 1 && x != roomSize.Right &&
                            y != roomSize.Bottom - 1 && y != roomSize.Top)
                        {
                            continue;
                        }

                        var index = (Vector2i) invTransform.Transform(new Vector2(x, y));

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
                for (var x = roomSize.Left - 1; x <= roomSize.Right; x++)
                {
                    for (var y = roomSize.Bottom - 1; y <= roomSize.Top; y++)
                    {
                        if (x != roomSize.Left - 1 && x != roomSize.Right &&
                            y != roomSize.Bottom - 1 && y != roomSize.Top)
                        {
                            continue;
                        }

                        var index = (Vector2i) invTransform.Transform(new Vector2(x, y));
                        var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(index);

                        // Occupied tile.
                        if (anchoredEnts.MoveNext(out _))
                            continue;

                        Spawn("WallSolid", grid.GridTileToLocal(index));
                    }
                }
                */
            }
        }

        _mapManager.DeleteMap(dummyMap);

        return dungeon;
    }
}
