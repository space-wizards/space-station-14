// ReSharper disable once RedundantUsingDirective
// Used to warn the player in big red letters in debug mode

using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
    class MappingCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "mapping";
        public string Description => "Creates and teleports you to a new uninitialized map for mapping.";
        public string Help => $"Usage: {Command} <mapname> / {Command} <id> <mapname>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("Only players can use this command");
                return;
            }

#if DEBUG
            shell.WriteError("WARNING: The server is using a debug build. You are risking losing your changes.");
#endif

            var mapManager = IoCManager.Resolve<IMapManager>();
            int mapId;
            string mapName;

            switch (args.Length)
            {
                case 1:
                    if (player.AttachedEntity == null)
                    {
                        shell.WriteError("The map name argument cannot be omitted if you have no entity.");
                        return;
                    }

                    mapId = (int) mapManager.NextMapId();
                    mapName = args[0];
                    break;
                case 2:
                    if (!int.TryParse(args[0], out var id))
                    {
                        shell.WriteError($"{args[0]} is not a valid integer.");
                        return;
                    }

                    mapId = id;
                    mapName = args[1];
                    break;
                default:
                    shell.WriteLine(Help);
                    return;
            }

            if (mapManager.MapExists(new MapId(mapId)))
            {
                shell.WriteLine($"Map {mapId} already exists");
                return;
            }

            shell.ExecuteCommand("sudo cvar events.enabled false");
            shell.ExecuteCommand($"addmap {mapId} false");
            shell.ExecuteCommand($"loadbp {mapId} \"{CommandParsing.Escape(mapName)}\" true");

            if (player.AttachedEntity is {Valid: true} playerEntity &&
                _entities.GetComponent<MetaDataComponent>(playerEntity).EntityPrototype?.ID != "AdminObserver")
            {
                shell.ExecuteCommand("aghost");
            }

            shell.ExecuteCommand($"tp 0 0 {mapId}");
            shell.RemoteExecuteCommand("showmarkers");

            var newGrid = mapManager.GetAllGrids().OrderByDescending(g => (int) g.Index).First();
            var pauseManager = IoCManager.Resolve<IPauseManager>();

            pauseManager.SetMapPaused(newGrid.ParentMapId, true);

            shell.WriteLine($"Created unloaded map from file {mapName} with id {mapId}. Use \"savebp {newGrid.Index} foo.yml\" to save the new grid as a map.");
        }
    }
}
