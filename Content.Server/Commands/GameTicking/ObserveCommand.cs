using Content.Server.Administration;
using Content.Server.Interfaces.GameTicking;
using Robust.Server.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.Commands.GameTicking
{
    [AnyCommand]
    class ObserveCommand : IServerCommand
    {
        public string Command => "observe";
        public string Description => "";
        public string Help => "";

        public void Execute(IServerConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();
            ticker.MakeObserve(player);
        }
    }
}