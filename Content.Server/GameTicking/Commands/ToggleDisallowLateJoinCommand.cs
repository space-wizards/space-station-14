using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Server)]
    class ToggleDisallowLateJoinCommand : IConsoleCommand
    {
        public string Command => "toggledisallowlatejoin";
        public string Description => "Allows or disallows latejoining during mid-game.";
        public string Help => $"Usage: {Command} <disallow>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("Need exactly one argument.");
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();

            if (bool.TryParse(args[0], out var result))
            {
                ticker.ToggleDisallowLateJoin(bool.Parse(args[0]));
                shell.WriteLine(result ? "Late joining has been disabled." : "Late joining has been enabled.");
            }
            else
            {
                shell.WriteLine("Invalid argument.");
            }
        }
    }
}
