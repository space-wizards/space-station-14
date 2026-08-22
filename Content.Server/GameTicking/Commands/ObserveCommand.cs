using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands
{
    [AnyCommand]
    sealed partial class ObserveCommand : IConsoleCommand
    {
        [Dependency] private IEntityManager _e = default!;
        [Dependency] private IAdminManager _adminManager = default!;
        [Dependency] private IConfigurationManager _cfg = default!;

        public string Command => "observe";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            var ticker = _e.System<GameTicker>();

            if (ticker.RunLevel == GameRunLevel.PreRoundLobby)
            {
                shell.WriteError("Wait until the round starts.");
                return;
            }

            var isAdmin = _adminManager.IsAdmin(player);
            var wantsAdmin = args.Length > 0 && args[0].Equals("admin", StringComparison.InvariantCultureIgnoreCase);

            if (isAdmin && !wantsAdmin && _cfg.GetCVar(CCVars.AdminDeadminOnJoin))
            {
                _adminManager.DeAdmin(player);
            }

            if (ticker.PlayerGameStatuses.TryGetValue(player.UserId, out var status) &&
                status != PlayerGameStatus.JoinedGame)
            {
                ticker.JoinAsObserver(player, isAdmin && wantsAdmin);
            }
            else
            {
                shell.WriteError($"{player.Name} is not in the lobby.   This incident will be reported.");
            }
        }
    }
}
