using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AnyCommand]
internal sealed class SuicideCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SuicideSystem _suicideSystem = default!;

    public override string Command => "suicide";

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
            var deniedMessage = Loc.GetString("suicide-command-denied");
            shell.WriteLine(deniedMessage);
            _popupSystem.PopupEntity(deniedMessage, frozen, frozen);
            return;
        }

        if (!_mindSystem.TryGetMind(player, out _, out var mindComp) ||
            mindComp.OwnedEntity is not { Valid: true } victim)
        {
            shell.WriteLine(Loc.GetString("suicide-command-no-mind"));
            return;
        }

        if (!_suicideSystem.Suicide(victim))
            shell.WriteLine(Loc.GetString("suicide-command-denied"));
    }
}
