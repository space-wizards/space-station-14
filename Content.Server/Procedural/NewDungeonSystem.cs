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
        var random = new System.Random(seed + 256);
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
            var offset = pack.BottomLeft;
            var dimensions = new Vector2i(pack.Width, pack.Height);
            var rotation = Angle.Zero;
            var flipped = false;

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
                            var index = new Vector2i(x + offset.X, y + offset.Y);
                            tiles.Add((index, new Tile(_tileDefManager["FloorSteel"].TileId)));
                        }
                    }

                    grid.SetTiles(tiles);
                    tiles.Clear();
                    continue;
                }

                // Well we have a rotated version
                rotation = new Angle(Math.PI / 2);
                offset = (Vector2i) rotation.RotateVec(offset);
                flipped = true;
                Logger.Debug($"Using rotated variant for rooms pack");
            }

            // Actual spawn cud here.
            // Pickout the room pack template to get the room dimensions we need.
            // TODO: Need to be able to load entities on top of other entities but das a lot of effo
            var gottem = packs[random.Next(packs.Count)];

            foreach (var roomSize in gottem.Rooms)
            {
                Vector2i roomDimensions;

                if (flipped)
                {
                    roomDimensions = new Vector2i(roomSize.Height, roomSize.Width);
                }
                else
                {
                    roomDimensions = new Vector2i(roomSize.Width, roomSize.Height);
                }

                if (!roomProtos.TryGetValue(roomDimensions, out var roomProto))
                {
                    roomDimensions = new Vector2i(roomDimensions.Y, roomDimensions.X);

                    if (!roomProtos.TryGetValue(roomDimensions, out roomProto))
                    {
                        Logger.Error($"Unable to find room variant for {roomDimensions}, leaving empty.");
                        continue;
                    }

                    Logger.Debug($"Using rotated variant for room");
                }

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
                    // TODO: Transform
                    tiles.Add((tile.Value.GridIndices, tile.Value.Tile));
                }

                // TODO: Copy contents across

                grid.SetTiles(tiles);
                tiles.Clear();

                // Now copy entities
                foreach (var )

                // TODO: Spawn doors

                // Spawn wall outline
                // - Tiles first
                for (var x = roomSize.Left - 1; x <= roomSize.Right; x++)
                {
                    for (var y = roomSize.Bottom - 1; y <= roomSize.Top; y++)
                    {
                        if (x != roomSize.Left - 1 && x != roomSize.Right &&
                            y != roomSize.Bottom - 1 && y != roomSize.Top)
                        {
                            continue;
                        }

                        var index = new Vector2i(x + offset.X, y + offset.Y);

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

                        var index = new Vector2i(x + offset.X, y + offset.Y);

                        var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(index);

                        // Occupied tile.
                        if (anchoredEnts.MoveNext(out _))
                            continue;

                        Spawn("WallSolid", grid.GridTileToLocal(index));
                    }
                }
            }
        }

        _mapManager.DeleteMap(dummyMap);

        return dungeon;
    }
}
