using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands
{
    [AnyCommand]
    sealed class ToggleReadyCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;

        public string Command => "toggleready";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }
            if (player == null)
            {
                return;
            }

            var ticker = _sysMan.GetEntitySystem<GameTicker>();
            ticker.ToggleReady(player, bool.Parse(args[0]));
        }
    }
}
