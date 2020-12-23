using Content.Server.Administration;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.Commands.GameTicking
{
    [AdminCommand(AdminFlags.Server)]
    class ToggleDisallowLateJoinCommand : IClientCommand
    {
        public string Command => "toggledisallowlatejoin";
        public string Description => "Allows or disallows latejoining during mid-game.";
        public string Help => $"Usage: {Command} <disallow>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Need exactly one argument.");
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();

            if (bool.TryParse(args[0], out var result))
            {
                ticker.ToggleDisallowLateJoin(bool.Parse(args[0]));
                shell.SendText(player, result ? "Late joining has been disabled." : "Late joining has been enabled.");
            }
            else
            {
                shell.SendText(player, "Invalid argument.");
            }
        }
    }
}
