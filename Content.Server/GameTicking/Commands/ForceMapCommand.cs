using Content.Server.Administration;
using Content.Server.Maps;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    sealed class ForceMapCommand : IConsoleCommand
    {
        public string Command => "forcemap";
        public string Description => "forcemap-command-description";
        public string Help => $"forcemap-command-help";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString("forcemap-command-need-one-argument"));
                return;
            }

            var gameMap = IoCManager.Resolve<IGameMapManager>();
            var name = args[0];

            gameMap.ForceSelectMap(name);
            shell.WriteLine(Loc.GetString("forcemap-command-success", ("map", name)));
        }
    }
}
