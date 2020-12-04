using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.Commands.GameTicking
{
    [AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
    class MappingCommand : IClientCommand
    {
        public string Command => "mapping";
        public string Description => "Creates and teleports you to a new uninitialized map for mapping.";
        public string Help => $"Usage: {Command} <mapname> / {Command} <id> <mapname>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                shell.SendText(player, "Only players can use this command");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            int mapId;
            string mapName;

            switch (args.Length)
            {
                case 1:
                    if (player.AttachedEntity == null)
                    {
                        shell.SendText(player, "The map name argument cannot be omitted if you have no entity.");
                        return;
                    }

                    mapId = (int) mapManager.NextMapId();
                    mapName = args[0];
                    break;
                case 2:
                    if (!int.TryParse(args[0], out var id))
                    {
                        shell.SendText(player, $"{args[0]} is not a valid integer.");
                        return;
                    }

                    mapId = id;
                    mapName = args[1];
                    break;
                default:
                    shell.SendText(player, Help);
                    return;
            }

            shell.ExecuteCommand(player, $"addmap {mapId} false");
            shell.ExecuteCommand(player, $"loadbp {mapId} \"{CommandParsing.Escape(mapName)}\"");
            shell.ExecuteCommand(player, "aghost");
            shell.ExecuteCommand(player, $"tp 0 0 {mapId}");

            var newGridId = mapManager.GetAllGrids().Max(g => (int) g.Index);

            shell.SendText(player, $"Created unloaded map from file {mapName} with id {mapId}. Use \"savebp {newGridId} foo.yml\" to save the new grid as a map.");
        }
    }
}