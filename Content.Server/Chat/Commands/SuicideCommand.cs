using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class SuicideCommand : IConsoleCommand
    {
        public string Command => "suicide";

        public string Description => Loc.GetString("suicide-command-description");

        public string Help => Loc.GetString("suicide-command-help-text");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteLine(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (player.Status != SessionStatus.InGame || player.AttachedEntity == null)
                return;

            var minds = IoCManager.Resolve<IEntityManager>().System<SharedMindSystem>();
            // This check also proves mind not-null for at the end when the mob is ghosted.
            if (!minds.TryGetMind(player, out var mindId, out var mind) ||
                mind.OwnedEntity is not { Valid: true } victim)
            {
                shell.WriteLine("You don't have a mind!");
                return;
            }

            var gameTicker = EntitySystem.Get<GameTicker>();
            var suicideSystem = EntitySystem.Get<SuicideSystem>();
            if (suicideSystem.Suicide(victim))
            {
                // Prevent the player from returning to the body.
                // Note that mind cannot be null because otherwise victim would be null.
                gameTicker.OnGhostAttempt(mindId, false, mind: mind);
                return;
            }

            if (gameTicker.OnGhostAttempt(mindId, true, mind: mind))
                return;

            shell.WriteLine("You can't ghost right now.");
        }
    }
}
