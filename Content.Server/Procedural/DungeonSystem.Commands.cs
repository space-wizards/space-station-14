using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    /// <summary>
    /// Generates a dungeon via command.
    /// </summary>
    [AdminCommand(AdminFlags.Fun)]
    private async void GenerateDungeon(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length < 4)
        {
            shell.WriteError("cmd-dungen-arg-count");
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
        {
            shell.WriteError("cmd-dungen-map-parse");
            return;
        }

        var mapId = new MapId(mapInt);

        if (!_prototype.TryIndex<DungeonConfigPrototype>(args[1], out var dungeon))
        {
            shell.WriteError(Loc.GetString("cmd-dungen-config"));
            return;
        }

        if (!int.TryParse(args[2], out var posX) || !int.TryParse(args[3], out var posY))
        {
            shell.WriteError(Loc.GetString("cmd-dungen-pos"));
            return;
        }

        var position = new Vector2i(posX, posY);
        var dungeonUid = _mapManager.GetMapEntityId(mapId);

        if (!TryComp<MapGridComponent>(dungeonUid, out var dungeonGrid))
        {
            dungeonUid = EntityManager.CreateEntityUninitialized(null, new EntityCoordinates(dungeonUid, position));
            dungeonGrid = EntityManager.AddComponent<MapGridComponent>(dungeonUid);
            EntityManager.InitializeAndStartEntity(dungeonUid, mapId);
            // If we created a grid (e.g. space dungen) then offset it so we don't double-apply positions
            position = Vector2i.Zero;
        }

        int seed;

        if (args.Length >= 5)
        {
            if (!int.TryParse(args[4], out seed))
            {
                shell.WriteError(Loc.GetString("cmd-dungen-seed"));
                return;
            }
        }
        else
        {
            seed = new Random().Next();
        }

        shell.WriteLine(Loc.GetString("cmd-dungen-start", ("seed", seed)));
        GenerateDungeon(dungeon, dungeonUid, dungeonGrid, position, seed);
    }

    private CompletionResult CompletionCallback(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), Loc.GetString("cmd-dungen-hint-map"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<DungeonConfigPrototype>(proto: _prototype), Loc.GetString("cmd-dungen-hint-config"));
        }

        if (args.Length == 3)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-dungen-hint-posx"));
        }

        if (args.Length == 4)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-dungen-hint-posy"));
        }

        if (args.Length == 5)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-dungen-hint-seed"));
        }

        return CompletionResult.Empty;
    }

    [AdminCommand(AdminFlags.Mapping)]
    private void DungeonPackVis(IConsoleShell shell, string argstr, string[] args)
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

        if (!_prototype.TryIndex<DungeonRoomPackPrototype>(args[1], out var pack))
        {
            return;
        }

        var grid = EnsureComp<MapGridComponent>(mapUid);
        var tile = new Tile(_tileDefManager["FloorSteel"].TileId);
        var tiles = new List<(Vector2i, Tile)>();

        foreach (var room in pack.Rooms)
        {
            for (var x = room.Left; x < room.Right; x++)
            {
                for (var y = room.Bottom; y < room.Top; y++)
                {
                    var index = new Vector2i(x, y);
                    tiles.Add((index, tile));
                }
            }
        }

        // Fill the rest out with a blank tile to make it easier to see
        var dummyTile = new Tile(_tileDefManager["FloorAsteroidIronsand1"].TileId);

        for (var x = 0; x < pack.Size.X; x++)
        {
            for (var y = 0; y < pack.Size.Y; y++)
            {
                var index = new Vector2i(x, y);
                if (tiles.Contains((index, tile)))
                    continue;

                tiles.Add((index, dummyTile));
            }
        }

        grid.SetTiles(tiles);
        shell.WriteLine(Loc.GetString("cmd-dungen_pack_vis"));
    }

    [AdminCommand(AdminFlags.Mapping)]
    private void DungeonPresetVis(IConsoleShell shell, string argstr, string[] args)
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

        if (!_prototype.TryIndex<DungeonPresetPrototype>(args[1], out var preset))
        {
            return;
        }

        var grid = EnsureComp<MapGridComponent>(mapUid);
        var tile = new Tile(_tileDefManager["FloorSteel"].TileId);
        var tiles = new List<(Vector2i, Tile)>();

        foreach (var room in preset.RoomPacks)
        {
            for (var x = room.Left; x < room.Right; x++)
            {
                for (var y = room.Bottom; y < room.Top; y++)
                {
                    var index = new Vector2i(x, y);
                    tiles.Add((index, tile));
                }
            }
        }

        grid.SetTiles(tiles);
        shell.WriteLine(Loc.GetString("cmd-dungen_pack_vis"));
    }

    private CompletionResult PresetCallback(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), Loc.GetString("cmd-dungen-hint-map"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromOptions(CompletionHelper.PrototypeIDs<DungeonPresetPrototype>(proto: _prototype));
        }

        return CompletionResult.Empty;
    }

    private CompletionResult PackCallback(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), Loc.GetString("cmd-dungen-hint-map"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromOptions(CompletionHelper.PrototypeIDs<DungeonRoomPackPrototype>(proto: _prototype));
        }

        return CompletionResult.Empty;
    }
}
