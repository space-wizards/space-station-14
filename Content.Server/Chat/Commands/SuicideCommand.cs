using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class SuicideCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly SuicideSystem _suicideSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override string Command => "suicide";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (player.Status != SessionStatus.InGame || player.AttachedEntity == null)
                return;

            // This check also proves mind not-null for at the end when the mob is ghosted.
            if (!_mindSystem.TryGetMind(player, out var mindId, out var mindComp) ||
                mindComp.OwnedEntity is not { Valid: true } victim)
            {
                shell.WriteLine(Loc.GetString("cmd-suicide-no-mind"));
                return;
            }

            if (EntityManager.HasComponent<AdminFrozenComponent>(victim))
            {
                var deniedMessage = Loc.GetString("cmd-suicide-denied");
                shell.WriteLine(deniedMessage);
                _popupSystem.PopupEntity(deniedMessage, victim, victim);
                return;
            }

            if (_suicideSystem.Suicide(victim))
                return;

            shell.WriteLine(Loc.GetString("cmd-ghost-denied"));
        }
    }
}
