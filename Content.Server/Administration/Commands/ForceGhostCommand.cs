using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ForceGhostCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "forceghost";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0 || args.Length > 1)
        {
            shell.WriteError(LocalizationManager.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_playerManager.TryGetSessionByUsername(args[0], out var player))
        {
            shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
            return;
        }

        var gameTicker = _entityManager.System<GameTicker>();
        if (!gameTicker.PlayerGameStatuses.TryGetValue(player.UserId, out var playerStatus) ||
            playerStatus is not PlayerGameStatus.JoinedGame)
        {
            shell.WriteLine(Loc.GetString("cmd-forceghost-error-lobby"));
            return;
        }

        var mindSys = _entityManager.System<SharedMindSystem>();
        if (!mindSys.TryGetMind(player, out var mindId, out var mind))
            (mindId, mind) = mindSys.CreateMind(player.UserId);

        var ghostSys = _entityManager.System<GhostSystem>();
        if (!ghostSys.OnGhostAttempt(mindId, false, true, true, mind))
            shell.WriteLine(Loc.GetString("cmd-forceghost-denied"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-forceghost-hint"));
        }

        return CompletionResult.Empty;
    }
}
