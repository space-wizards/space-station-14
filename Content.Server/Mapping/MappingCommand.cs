// ReSharper disable once RedundantUsingDirective
// Used to warn the player in big red letters in debug mode

using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Mapping
{
    [AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
    sealed class MappingCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "mapping";
        public string Description => Loc.GetString("cmd-mapping-desc");
        public string Help => Loc.GetString("cmd-mapping-help");

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-mapping-id"));
                case 2:
                    var res = IoCManager.Resolve<IResourceManager>();
                    var opts = CompletionHelper.UserFilePath(args[1], res.UserData)
                        .Concat(CompletionHelper.ContentFilePath(args[1], res));
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-mapping-path"));
            }
            return CompletionResult.Empty;
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("cmd-savemap-server"));
                return;
            }

            if (args.Length > 2)
            {
                shell.WriteLine(Help);
                return;
            }

#if DEBUG
            shell.WriteError(Loc.GetString("cmd-mapping-warning"));
#endif

            var mapManager = IoCManager.Resolve<IMapManager>();
            MapId mapId;

            // Get the map ID to use
            if (args.Length is 1 or 2)
            {

                if (!int.TryParse(args[0], out var intMapId))
                {
                    shell.WriteError(Loc.GetString("cmd-mapping-failure-integer", ("arg", args[0])));
                    return;
                }

                mapId = new MapId(intMapId);

                // no loading null space
                if (mapId == MapId.Nullspace)
                {
                    shell.WriteError(Loc.GetString("cmd-mapping-nullspace"));
                    return;
                }

                if (mapManager.MapExists(mapId))
                {
                    shell.WriteError(Loc.GetString("cmd-mapping-exists", ("mapId", mapId)));
                    return;
                }

            }
            else
            {
                mapId = mapManager.NextMapId();
            }

            string? toLoad = null;
            // either load a map or create a new one.
            if (args.Length <= 1)
            {
                shell.ExecuteCommand($"addmap {mapId} false");
            }
            else
            {
                toLoad = CommandParsing.Escape(args[1]);
                shell.ExecuteCommand($"loadmap {mapId} \"{toLoad}\" 0 0 0 true");
            }

            // was the map actually created?
            if (!mapManager.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-mapping-error"));
                return;
            }

            // map successfully created. run misc helpful mapping commands
            if (player.AttachedEntity is { Valid: true } playerEntity &&
                _entities.GetComponent<MetaDataComponent>(playerEntity).EntityPrototype?.ID != GameTicker.AdminObserverPrototypeName)
            {
                shell.ExecuteCommand("aghost");
            }

            var cfg = IoCManager.Resolve<IConfigurationManager>();

            // don't interrupt mapping with events or auto-shuttle
            shell.ExecuteCommand("sudo cvar events.enabled false");
            shell.ExecuteCommand("sudo cvar shuttle.auto_call_time 0");

            if (cfg.GetCVar(CCVars.AutosaveEnabled))
                shell.ExecuteCommand($"toggleautosave {mapId} {toLoad ?? "NEWMAP"}");
            shell.ExecuteCommand($"tp 0 0 {mapId}");
            shell.RemoteExecuteCommand("mappingclientsidesetup");
            mapManager.SetMapPaused(mapId, true);

            if (args.Length == 2)
                shell.WriteLine(Loc.GetString("cmd-mapping-success-load",("mapId",mapId),("path", args[1])));
            else
                shell.WriteLine(Loc.GetString("cmd-mapping-success", ("mapId", mapId)));
        }
    }
}
