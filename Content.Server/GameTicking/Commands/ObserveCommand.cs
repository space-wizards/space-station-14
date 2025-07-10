using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands;

[AnyCommand]
public sealed class ObserveCommand : LocalizedEntityCommands
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

        if ((_gameTicker.PlayerGameStatuses.TryGetValue(player.UserId, out var status) &&
             status != PlayerGameStatus.JoinedGame) ||
            _gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
        {
            shell.WriteError(Loc.GetString("shell-can-only-run-from-in-round-lobby"));
            return;
        }

        if (args.Length == 1)
        {
            if (!bool.TryParse(args[0], out var enabled))
            {
                shell.WriteError(Loc.GetString("shell-invalid-bool"));
                return;
            }

            if (!enabled)
                _adminManager.IsAdmin(player);
        }

        _gameTicker.JoinAsObserver(player);
    }
}
