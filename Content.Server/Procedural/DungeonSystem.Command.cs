using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    private void InitializeCommand()
    {
        _console.RegisterCommand("dungen", $"A", "B", DunGenCommand);
    }

    [AdminCommand(AdminFlags.Mapping)]
    private void DunGenCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
        {
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
        {
            return;
        }

        var mapId = new MapId(mapInt);

        if (!_mapManager.MapExists(mapId))
        {
            return;
        }

        var bsp = new BSPDunGen()
        {
            Bounds = new Box2i(Vector2i.Zero, new Vector2i(70, 70)),
            MinimumRoomDimensions = new Vector2i(10, 10),
        };

        var gen = GetBSPDungeon(bsp);
        var mapUid = _mapManager.GetMapEntityId(mapId);
        var grid = EnsureComp<MapGridComponent>(mapUid);
        var tiles = new List<(Vector2i, Tile)>();
        var bottomText = (ContentTileDefinition) _tileDef["FloorSteel"];

        foreach (var room in gen.Rooms)
        {
            foreach (var tile in room.Tiles)
            {
                tiles.Add((tile, new Tile(bottomText.TileId)));
            }
        }

        foreach (var tile in gen.Corridors)
        {
            tiles.Add((tile, new Tile(bottomText.TileId)));
        }

        foreach (var tile in gen.Walls)
        {
            tiles.Add((tile, new Tile(bottomText.TileId)));
        }

        grid.SetTiles(tiles);

        foreach (var tile in gen.Walls)
        {
            Spawn("WallSolid", grid.GridTileToLocal(tile));
        }
    }
}
