using System.Linq;
using Content.Server.Administration;
using Content.Server.Maps;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    sealed class ForceMapCommand : IConsoleCommand
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        public string Command => "forcemap";
        public string Description => Loc.GetString("forcemap-command-description");
        public string Help => Loc.GetString("forcemap-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString("forcemap-command-need-one-argument"));
                return;
            }

            var gameMap = IoCManager.Resolve<IGameMapManager>();
            var name = args[0];

            _configurationManager.SetCVar(CCVars.GameMap, name);
            shell.WriteLine(Loc.GetString("forcemap-command-success", ("map", name)));
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = IoCManager.Resolve<IPrototypeManager>()
                    .EnumeratePrototypes<GameMapPrototype>()
                    .Select(p => new CompletionOption(p.ID, p.MapName))
                    .OrderBy(p => p.Value);

                return CompletionResult.FromHintOptions(options, Loc.GetString("forcemap-command-arg-map"));
            }

            return CompletionResult.Empty;
        }
    }
}
