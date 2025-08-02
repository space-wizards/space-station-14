using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Console;

namespace Content.Server.Ghost;

[AnyCommand]
public sealed class GhostCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override string Command => "ghost";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (!_gameTicker.PlayerGameStatuses.TryGetValue(player.UserId, out var playerStatus) ||
            playerStatus is not PlayerGameStatus.JoinedGame)
        {
            shell.WriteLine(Loc.GetString("suicide-command-error-lobby"));
            return;
        }


        if (player.AttachedEntity is { Valid: true } frozen &&
            EntityManager.HasComponent<AdminFrozenComponent>(frozen))
        {
            var deniedMessage = Loc.GetString("ghost-command-denied");
            shell.WriteLine(deniedMessage);
            _popupSystem.PopupEntity(deniedMessage, frozen, frozen);
            return;
        }

        if (!_mindSystem.TryGetMind(player, out var mindId, out var mind))
        {
            mindId = _mindSystem.CreateMind(player.UserId);
            mind = EntityManager.GetComponent<MindComponent>(mindId);
        }

        if (!_ghostSystem.OnGhostAttempt(mindId, true, true, mind: mind))
            shell.WriteLine(Loc.GetString("ghost-command-denied"));
    }
}
