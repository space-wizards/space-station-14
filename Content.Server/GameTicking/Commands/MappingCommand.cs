// ReSharper disable once RedundantUsingDirective
// Used to warn the player in big red letters in debug mode

using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
    sealed class MappingCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "mapping";
        public string Description => "Creates and teleports you to a new uninitialized map for mapping.";
        public string Help => $"Usage: {Command} <MapID> <Path>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not IPlayerSession player)
            {
                shell.WriteError("Only players can use this command");
                return;
            }

            if (args.Length > 2)
            {
                shell.WriteLine(Help);
                return;
            }

#if DEBUG
            shell.WriteError("WARNING: The server is using a debug build. You are risking losing your changes.");
#endif

            var mapManager = IoCManager.Resolve<IMapManager>();
            MapId mapId;

            // Get the map ID to use
            if (args.Length is 1 or 2)
            {
                if (!int.TryParse(args[0], out var id))
                {
                    shell.WriteError($"{args[0]} is not a valid integer.");
                    return;
                }

                mapId = new MapId(id);
                if (mapManager.MapExists(mapId))
                {
                    shell.WriteError($"Map {mapId} already exists");
                    return;
                }
            }
            else
            {
                mapId = mapManager.NextMapId();
            }

            DebugTools.Assert(args.Length <= 2);

            // either load a map or create a new one.
            if (args.Length <= 1)
                shell.ExecuteCommand($"addmap {mapId} false");
            else
                shell.ExecuteCommand($"loadmap {mapId} \"{CommandParsing.Escape(args[1])}\"");

            // was the map actually created?
            if (!mapManager.MapExists(mapId))
            {
                shell.WriteError($"An error occurred when creating the new map.");
                return;
            }

            // map successfully created. run misc helpful mapping commands
            if (player.AttachedEntity is { Valid: true } playerEntity &&
                _entities.GetComponent<MetaDataComponent>(playerEntity).EntityPrototype?.ID != "AdminObserver")
            {
                shell.ExecuteCommand("aghost");
            }

            shell.ExecuteCommand("sudo cvar events.enabled false");
            shell.ExecuteCommand($"tp 0 0 {mapId}");
            shell.RemoteExecuteCommand("showmarkers");
            shell.RemoteExecuteCommand("togglelight");
            shell.RemoteExecuteCommand("showsubfloorforever");
            shell.RemoteExecuteCommand("loadmapacts");
            mapManager.SetMapPaused(mapId, true);

            if (args.Length == 2)
                shell.WriteLine($"Created uninitialized map from file {args[1]} with id {mapId}.");
            else
                shell.WriteLine($"Created a new uninitialized map with id {mapId}.");
        }
    }
}
