using System.Linq;
using Content.Server.Administration;
using Content.Server.Maps;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Ronstation.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    public sealed class ForceCCCommand : LocalizedCommands
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IGameMapManager _gameMapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Command => "forcecc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString(Loc.GetString($"shell-need-exactly-one-argument")));
                return;
            }

            var name = args[0];

            // An empty string clears the forced map
            if (!string.IsNullOrEmpty(name) && !_gameMapManager.CheckMapExists(name))
            {
                shell.WriteLine(Loc.GetString("cmd-forcecc-map-not-found", ("map", name)));
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                _configurationManager.SetCVar(CCVars.CentComm, "");
                shell.WriteLine(Loc.GetString("cmd-forcecc-cleared"));
                return;
            }

            var map = _prototypeManager.Index<GameMapPrototype>(name);
            if (map == null)
            {
                shell.WriteLine(Loc.GetString("cmd-forcecc-map-not-found", ("map", name)));
                return;
            }

            if (!map.IsGrid)
            {
                shell.WriteLine(Loc.GetString("cmd-forcecc-map-is-not-grid", ("map", name)));
                return;
            }

            _configurationManager.SetCVar(CCVars.CentComm, map.MapPath.ToString());
            shell.WriteLine(Loc.GetString("cmd-forcecc-success", ("map", name)));
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = _prototypeManager
                    .EnumeratePrototypes<GameMapPrototype>()
                    .Where(p => p.IsGrid)
                    .Select(p => new CompletionOption(p.ID, p.MapName))
                    .OrderBy(p => p.Value);

                return CompletionResult.FromHintOptions(options, Loc.GetString($"cmd-forcecc-hint"));
            }

            return CompletionResult.Empty;
        }
    }
}
