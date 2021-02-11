// ReSharper disable once RedundantUsingDirective
// Used to warn the player in big red letters in debug mode
using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Player;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.Commands.GameTicking
{
    [AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
    class MappingCommand : IConsoleCommand
    {
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
                        shell.WriteLine("The map name argument cannot be omitted if you have no entity.");
                        return;
                    }

                    mapId = (int) mapManager.NextMapId();
                    mapName = args[0];
                    break;
                case 2:
                    if (!int.TryParse(args[0], out var id))
                    {
                        shell.WriteLine($"{args[0]} is not a valid integer.");
                        return;
                    }

                    mapId = id;
                    mapName = args[1];
                    break;
                default:
                    shell.WriteLine(Help);
                    return;
            }

            shell.ExecuteCommand($"addmap {mapId} false");
            shell.ExecuteCommand($"loadbp {mapId} \"{CommandParsing.Escape(mapName)}\" true");
            shell.ExecuteCommand("aghost");
            shell.ExecuteCommand($"tp 0 0 {mapId}");

            var newGrid = mapManager.GetAllGrids().OrderByDescending(g => (int) g.Index).First();
            var pauseManager = IoCManager.Resolve<IPauseManager>();

            pauseManager.SetMapPaused(newGrid.ParentMapId, true);

            shell.WriteLine($"Created unloaded map from file {mapName} with id {mapId}. Use \"savebp {newGrid.Index} foo.yml\" to save the new grid as a map.");
        }
    }
}
