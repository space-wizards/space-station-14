using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
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
            Bounds = new Box2i(Vector2i.Zero, new Vector2i(100, 100))
        };
        var gen = GetBSPDungeon(bsp);
        var grid = EnsureComp<MapGridComponent>(_mapManager.GetMapEntityId(mapId));
        var tiles = new List<(Vector2i, Tile)>();
        var bottomText = (ContentTileDefinition) _tileDef["FloorSteel"];

        foreach (var room in gen.Rooms)
        {
            foreach (var tile in room.Tiles)
            {
                tiles.Add((tile, new Tile(bottomText.TileId)));
            }
        }

        grid.SetTiles(tiles);
    }
}
