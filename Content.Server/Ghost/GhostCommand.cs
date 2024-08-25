using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Console;

namespace Content.Server.Ghost
{
    [AnyCommand]
    public sealed class GhostCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;

        public string Command => "ghost";
        public string Description => Loc.GetString("ghost-command-description");
        public string Help => Loc.GetString("ghost-command-help-text");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString("ghost-command-no-session"));
                return;
            }

            if (player.AttachedEntity is { Valid: true } frozen &&
                _entities.HasComponent<AdminFrozenComponent>(frozen))
            {
                var deniedMessage = Loc.GetString("ghost-command-denied");
                shell.WriteLine(deniedMessage);
                _entities.System<PopupSystem>()
                    .PopupEntity(deniedMessage, frozen, frozen);
                return;
            }

            if (_ticker.PlayerGameStatuses.TryGetValue(player.UserId, out var status) &&
                status != PlayerGameStatus.JoinedGame)
            {
                shell.WriteLine(Loc.GetString("ghost-command-lobby"));
                return;
            }

            if (!_mind.TryGetMind(player, out var mindId, out var mind))
            {
                mindId = _mind.CreateMind(player.UserId);
                mind = _entities.GetComponent<MindComponent>(mindId);
            }

            if (!_ticker.OnGhostAttempt(mindId, true, true, mind))
            {
                shell.WriteLine(Loc.GetString("ghost-command-denied"));
            }
        }
    }
}
