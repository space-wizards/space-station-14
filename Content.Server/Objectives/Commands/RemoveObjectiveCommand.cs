using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Objectives.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RemoveObjectiveCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

        public override string Command => "rmobjective";
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString(Loc.GetString("cmd-rmobjective-invalid-args")));
                return;
            }

            if (!_players.TryGetSessionByUsername(args[0], out var session))
            {
                shell.WriteError(Loc.GetString("cmd-rmojective-player-not-found"));
                return;
            }

            if (!_mind.TryGetMind(session, out var mindId, out var mind))
            {
                shell.WriteError(Loc.GetString("cmd-rmojective-mind-not-found"));
                return;
            }

            if (int.TryParse(args[1], out var i))
            {
                shell.WriteLine(Loc.GetString(_mind.TryRemoveObjective(mindId, mind, i)
                    ? "cmd-rmobjective-success"
                    : "cmd-rmobjective-failed"));
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-rmobjective-invalid-index", ("index", args[1])));
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), LocalizationManager.GetString("shell-argument-username-hint"));
            }
            if (args.Length == 2)
            {
                if (!_players.TryGetSessionByUsername(args[0], out var session))
                    return CompletionResult.Empty;

                if (!_mind.TryGetMind(session, out var mindId, out var mind))
                    return CompletionResult.Empty;

                if (mind.Objectives.Count == 0)
                    return CompletionResult.Empty;

                var options = new List<CompletionOption>();
                for (int i = 0; i < mind.Objectives.Count; i++)
                {
                    var info = _objectives.GetInfo(mind.Objectives[i], mindId, mind);
                    var hint = info == null ? Loc.GetString("cmd-rmobjective-invalid-objective-info") : $"{mind.Objectives[i]} ({info.Value.Title})";
                    options.Add(new CompletionOption(i.ToString(), hint));
                }

                return CompletionResult.FromOptions(options);
            }

            return CompletionResult.Empty;
        }
    }
}
