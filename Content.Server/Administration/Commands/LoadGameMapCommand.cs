using Content.Server.Maps;
using Content.Server.Roles;
using Content.Shared.Administration;
using Robust.Server.Maps;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class LoadGameMapCommand : IConsoleCommand
    {
        public string Command => "loadgamemap";

        public string Description => "Loads the given game map at the given coordinates.";

        public string Help => "loadgamemap <gamemap> <mapid> [<x> <y>]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var mapLoader = IoCManager.Resolve<IMapLoader>();
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var stationSystem = entityManager.EntitySysManager.GetEntitySystem<StationSystem>();

            if (args.Length is not (2 or 4))
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (prototypeManager.TryIndex<GameMapPrototype>(args[0], out var gameMap))
            {
                if (int.TryParse(args[1], out var mapId))
                {
                    var gameMapEnt = mapLoader.LoadBlueprint(new MapId(mapId), gameMap.MapPath);
                    if (gameMapEnt is null)
                    {
                        shell.WriteError($"Failed to create the given game map, is the path {gameMap.MapPath} correct?");
                        return;
                    }

                    if (args.Length is 4 && int.TryParse(args[2], out var x) && int.TryParse(args[3], out var y))
                    {
                        var transform = entityManager.GetComponent<TransformComponent>(gameMapEnt.GridEntityId);
                        transform.WorldPosition = new Vector2(x, y);
                    }

                    stationSystem.InitialSetupStationGrid(gameMapEnt.GridEntityId, gameMap);
                }
            }
            else
            {
                shell.WriteError($"The given map prototype {args[0]} is invalid.");
            }
        }
    }
}
