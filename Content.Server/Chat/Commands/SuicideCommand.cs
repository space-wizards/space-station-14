using Content.Server.Commands;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AnyCommand]
internal sealed class SuicideCommand : LocalizedEntityCommands
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SuicideSystem _suicideSystem = default!;

    public override string Command => "suicide";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!CommandChecks.MustBeAttachedToEntity(shell, out var player, out _))
            return;

        // This check also proves mind not-null for at the end when the mob is ghosted.
        if (!_mindSystem.TryGetMind(player, out _, out var mindComp) ||
            mindComp.OwnedEntity is not { Valid: true } victim)
        {
            shell.WriteLine(Loc.GetString("suicide-command-no-mind"));
            return;
        }

        if (EntityManager.HasComponent<AdminFrozenComponent>(victim))
        {
            var deniedMessage = Loc.GetString("suicide-command-denied");
            shell.WriteLine(deniedMessage);
            _popupSystem.PopupEntity(deniedMessage, victim, victim);
            return;
        }

        if (_suicideSystem.Suicide(victim))
            return;

        shell.WriteLine(Loc.GetString("ghost-command-denied"));
    }
}
