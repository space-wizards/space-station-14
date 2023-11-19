using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Console;

namespace Content.Server.Ghost
{
    [AnyCommand]
    public sealed class Ghost : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "ghost";
        public string Description => "Give up on life and become a ghost.";
        public string Help => "ghost";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine("You have no session, you can't ghost.");
                return;
            }

            //SS220-lobby-ghost-bug begin
            var gameTicker = _entities.System<GameTicker>();
            if (!gameTicker.PlayerGameStatuses.TryGetValue(player.UserId, out var status) || status is not PlayerGameStatus.JoinedGame)
            {
                shell.WriteLine("You can't ghost right now. You are not in the game!");
                return;
            }
            //SS220-lobby-ghost-bug end

            var minds = _entities.System<SharedMindSystem>();
            if (!minds.TryGetMind(player, out var mindId, out var mind))
            {
                mindId = minds.CreateMind(player.UserId);
                mind = _entities.GetComponent<MindComponent>(mindId);
            }

            if (!EntitySystem.Get<GameTicker>().OnGhostAttempt(mindId, true, true, mind))
            {
                shell.WriteLine("You can't ghost right now.");
            }
        }
    }
}
