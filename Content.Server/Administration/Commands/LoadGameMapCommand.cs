using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Round | AdminFlags.Spawn)]
    public sealed class LoadGameMapCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;

        public override string Command => "loadgamemap";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length is not (2 or 4 or 5))
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!_prototypeManager.TryIndex<GameMapPrototype>(args[1], out var gameMap))
            {
                shell.WriteError($"The given map prototype {args[0]} is invalid.");
                return;
            }

            if (!int.TryParse(args[0], out var mapId))
                return;

            var stationName = args.Length == 5 ? args[4] : null;

            Vector2? offset = null;
            if (args.Length >= 4)
                offset = new Vector2(int.Parse(args[2]), int.Parse(args[3]));

            var id = new MapId(mapId);

            var grids = _mapSystem.MapExists(id)
                ? _gameTicker.MergeGameMap(gameMap, id, stationName: stationName, offset: offset)
                : _gameTicker.LoadGameMapWithId(gameMap, id, stationName: stationName, offset: offset);

            shell.WriteLine($"Loaded {grids.Count} grids.");
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-savemap-id"));
                case 2:
                    var opts = CompletionHelper.PrototypeIDs<GameMapPrototype>();
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
                case 3:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-loadmap-x-position"));
                case 4:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-loadmap-y-position"));
                case 5:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-loadmap-rotation"));
                case 6:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-loadmap-uids"));
            }

            return CompletionResult.Empty;
        }
    }

    [AdminCommand(AdminFlags.Round | AdminFlags.Spawn)]
    public sealed class ListGameMaps : LocalizedCommands
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Command => "listgamemaps";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }
            foreach (var prototype in _prototypeManager.EnumeratePrototypes<GameMapPrototype>())
            {
                shell.WriteLine($"{prototype.ID} - {prototype.MapName}");
            }
        }
    }
}
