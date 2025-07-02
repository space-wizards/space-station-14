using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Objectives.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class ListObjectivesCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectiveSystem = default!;

    public override string Command => "lsobjectives";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        ICommonSession? player;
        if (args.Length > 0)
            _playerManager.TryGetSessionByUsername(args[0], out player);
        else
            player = shell.Player;

        if (player == null)
        {
            shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        if (!_mindSystem.TryGetMind(player, out var mindId, out var mind))
        {
            shell.WriteError(Loc.GetString("shell-target-entity-does-not-have-message", ("missing", "mind")));
            return;
        }

        var objectives = mind.Objectives.ToList();
        shell.WriteLine(objectives.Count == 0
            ? Loc.GetString("cmd-lsobjectives-no-objectives", ("player", player.UserId))
            : Loc.GetString("cmd-lsobjectives-for-player", ("player", player.UserId)));

        for (var i = 0; i < objectives.Count; i++)
        {
            var info = _objectiveSystem.GetInfo(objectives[i], mindId, mind);
            if (info == null)
                shell.WriteLine($"- [{i}] {objectives[i]} - INVALID");

            else
            {
                var progress = (int) (info.Value.Progress * 100f);
                shell.WriteLine($"- [{i}] {objectives[i]} ({info.Value.Title}) ({progress}%)");
            }
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1 ? CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("shell-argument-username-hint")) : CompletionResult.Empty;
    }
}
