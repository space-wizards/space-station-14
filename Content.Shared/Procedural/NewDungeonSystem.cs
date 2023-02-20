using Content.Shared.Procedural.RoomGens;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Procedural;

public sealed class NewDungeonSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;

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

        // TODO: Need to support rotation variants.
        foreach (var pack in gen.RoomPacks)
        {
            var dimensions = new Vector2i(pack.Width, pack.Height);

            // Fallback tiles for debug.
            if (!roomPackProtos.TryGetValue(dimensions, out var packs))
            {
                for (var x = pack.Left; x < pack.Right; x++)
                {
                    for (var y = pack.Bottom; y < pack.Top; y++)
                    {
                        var index = new Vector2i(x, y);
                        tiles.Add((index, new Tile(_tileDefManager["FloorSteel"].TileId)));
                    }
                }

                grid.SetTiles(tiles);
                tiles.Clear();
                continue;
            }

            // Actual spawn cud here.
            var gottem = packs[random.Next(packs.Count)];

            foreach (var room in gottem.Rooms)
            {
                var offset = pack.BottomLeft;
                tiles.Clear();
                var roomDimensions = new Vector2i(room.Width, room.Height);

                // TODO: Spawn doors

                // Spawn wall outline
                for (var x = room.Left - 1; x <= room.Right; x++)
                {
                    for (var y = room.Bottom - 1; y <= room.Top; y++)
                    {
                        if (x != room.Left - 1 && x != room.Right &&
                            y != room.Bottom - 1 && y != room.Top)
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
                for (var x = room.Left - 1; x <= room.Right; x++)
                {
                    for (var y = room.Bottom - 1; y <= room.Top; y++)
                    {
                        if (x != room.Left - 1 && x != room.Right &&
                            y != room.Bottom - 1 && y != room.Top)
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

                for (var x = room.Left; x < room.Right; x++)
                {
                    for (var y = room.Bottom; y < room.Top; y++)
                    {
                        var index = new Vector2i(x + offset.X, y + offset.Y);
                        tiles.Add((index, new Tile(_tileDefManager["FloorSteel"].TileId)));
                    }
                }

                grid.SetTiles(tiles);
            }
        }

        return dungeon;
    }
}
