using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Shared.Administration;
using Robust.Server.Maps;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Round | AdminFlags.Spawn)]
    public sealed class LoadGameMapCommand : IConsoleCommand
    {
        public string Command => "loadgamemap";

        public string Description => "Loads the given game map at the given coordinates.";

        public string Help => "loadgamemap <gamemap> <mapid> [<x> <y> [<name>]] ";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var gameTicker = entityManager.EntitySysManager.GetEntitySystem<GameTicker>();

            if (args.Length is not (2 or 4 or 5))
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (prototypeManager.TryIndex<GameMapPrototype>(args[0], out var gameMap))
            {
                if (!int.TryParse(args[1], out var mapId)) return;

                var loadOptions = new MapLoadOptions();
                var stationName = args.Length == 5 ? args[4] : null;

                if (args.Length >= 4 && int.TryParse(args[2], out var x) && int.TryParse(args[3], out var y))
                {
                    loadOptions.Offset = new Vector2(x, y);
                }
                var (ents, grids) = gameTicker.LoadGameMap(gameMap, new MapId(mapId), loadOptions, stationName);
                shell.WriteLine($"Loaded {ents.Count} entities and {grids.Count} grids.");
            }
            else
            {
                shell.WriteError($"The given map prototype {args[0]} is invalid.");
            }
        }
    }
}
