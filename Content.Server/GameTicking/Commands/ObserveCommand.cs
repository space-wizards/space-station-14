using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.Commands
{
    [AnyCommand]
    class ObserveCommand : IConsoleCommand
    {
        public string Command => "observe";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                return;
            }

            var ticker = EntitySystem.Get<GameTicker>();
            if (ticker.PlayersInLobby.ContainsKey(player))
                ticker.MakeObserve(player);
            else
                shell.WriteError($"{player.Name} is not in the lobby.   This incident will be reported.");
        }
    }
}
