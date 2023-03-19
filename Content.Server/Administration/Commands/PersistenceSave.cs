using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Server)]
    public sealed class PersistenceSave : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IEntitySystemManager _system = default!;
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly IConfigurationManager _configuration = default!;

        public string Command => "persistencesave";
        public string Description => "Saves server data.";
        public string Help => "persistencesave [mapId] [filePath - default: game.map (CCVar) ]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!int.TryParse(args[0], out var intMapId))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-integer", ("arg", args[0])));
                return;
            }

            var mapId = new MapId(intMapId);

            if (!_map.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-savemap-not-exist"));
                return;
            }

            var saveFilePath = (args.Length > 1 ? args[1] : null) ?? _configuration.GetCVar(CCVars.GameMap);
            if (string.IsNullOrWhiteSpace(saveFilePath))
            {
                // @TODO: Translation
                shell.WriteError($"filePath was not specified and CCVar {nameof(CCVars.GameMap)} is not set. Manually set the filePath param in order to save the map.");
                return;
            }

            var mapLoaderSys = _system.GetEntitySystem<MapLoaderSystem>();
            mapLoaderSys.SaveMap(mapId, saveFilePath);
            shell.WriteLine(Loc.GetString("cmd-savemap-success"));
        }
    }
}
