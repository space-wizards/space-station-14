using Content.Server.Administration;
using Content.Server.Interfaces.GameTicking;
using Robust.Server.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.Commands.GameTicking
{
    [AnyCommand]
    class ToggleReadyCommand : IServerCommand
    {
        public string Command => "toggleready";
        public string Description => "";
        public string Help => "";

        public void Execute(IServerConsoleShell shell, string argStr, string[] args)
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
