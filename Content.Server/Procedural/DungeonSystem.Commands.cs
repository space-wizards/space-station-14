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
        var mapUid = _mapManager.GetMapEntityId(mapId);

        if (!TryComp<MapGridComponent>(mapUid, out var mapGrid))
        {
            shell.WriteError(Loc.GetString("cmd-dungen-mapgrid"));
            return;
        }

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

        var position = new Vector2(posX, posY);
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
        GenerateDungeon(dungeon, mapUid, mapGrid, position, seed);
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
}
