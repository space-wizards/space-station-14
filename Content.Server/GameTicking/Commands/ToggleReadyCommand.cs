using Content.Server.Administration;
using Content.Server.Interfaces.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Server.Commands.GameTicking
{
    [AnyCommand]
    class ToggleReadyCommand : IConsoleCommand
    {
        public string Command => "toggleready";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();
            ticker.ToggleReady(player, bool.Parse(args[0]));
        }
    }
}
