using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Objectives.Commands
{
    [AdminCommand(AdminFlags.Logs)]
    public sealed class ListObjectivesCommand : LocalizedEntityCommands
        {
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly SharedObjectivesSystem _objectivesSystem = default!;
        [Dependency] private readonly IPlayerManager _players = default!;

        public override string Command => "lsobjectives";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            ICommonSession? player;
            if (args.Length > 0)
                _players.TryGetSessionByUsername(args[0], out player);
            else
                player = shell.Player;

            if (player == null)
            {
                shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
                return;
            }

            if (!_mindSystem.TryGetMind(player, out var mindId, out var mind))
            {
                shell.WriteError(LocalizationManager.GetString("shell-target-entity-does-not-have-message", ("missing", "mind")));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-lsobjectives-objectives-for-player", ("player", player.UserId)));
            var objectives = mind.Objectives.ToList();
            if (objectives.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-lsobjectives-none"));
            }

            for (var i = 0; i < objectives.Count; i++)
            {
                var info = _objectivesSystem.GetInfo(objectives[i], mindId, mind);
                if (info == null)
                {
                    shell.WriteLine(Loc.GetString("cmd-lsobjectives-invalid", ("objective", objectives[i])));
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
                return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("shell-argument-username-hint"));
            }

            return CompletionResult.Empty;
        }
    }
}
