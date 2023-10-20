using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Objectives.Commands
{
    [AdminCommand(AdminFlags.Logs)]
    public sealed class ListObjectivesCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IPlayerManager _players = default!;

        public override string Command => "lsobjectives";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null || !_players.TryGetSessionByUsername(args[0], out player))
            {
                shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
                return;
            }

            var minds = _entities.System<SharedMindSystem>();
            if (!minds.TryGetMind(player, out var mindId, out var mind))
            {
                shell.WriteError(LocalizationManager.GetString("shell-target-entity-does-not-have-message", ("missing", "mind")));
                return;
            }

            shell.WriteLine($"Objectives for player {player.UserId}:");
            var objectives = mind.AllObjectives.ToList();
            if (objectives.Count == 0)
            {
                shell.WriteLine("None.");
            }

            var objectivesSystem = _entities.System<SharedObjectivesSystem>();
            for (var i = 0; i < objectives.Count; i++)
            {
                var info = objectivesSystem.GetInfo(objectives[i], mindId, mind);
                if (info == null)
                {
                    shell.WriteLine($"- [{i}] {objectives[i]} - INVALID");
                }
                else
                {

                    var progress = (int) (info.Value.Progress * 100f);
                    shell.WriteLine($"- [{i}] {objectives[i]} ({info.Value.Title}) ({progress}%)");
                }
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), LocalizationManager.GetString("shell-argument-username-hint"));
            }

            return CompletionResult.Empty;
        }
    }
}
