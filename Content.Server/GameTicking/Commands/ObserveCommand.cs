using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands
{
    [AnyCommand]
    sealed class ObserveCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;

        public override string Command => "observe";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            {
                shell.WriteError(Loc.GetString("shell-can-only-run-while-round-is-active"));
                return;
            }

            var isAdminCommand = args.Length > 0 && args[0].ToLower() == "admin";

            if (!isAdminCommand && _adminManager.IsAdmin(player))
            {
                _adminManager.DeAdmin(player);
            }

            if (_gameTicker.PlayerGameStatuses.TryGetValue(player.UserId, out var status) &&
                status != PlayerGameStatus.JoinedGame)
            {
                _gameTicker.JoinAsObserver(player);
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-observe-not-in-lobby", ("player", player.Name)));
            }
        }
    }
}
