using Content.Shared.Administration;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class RPGoalInfoCommand : LocalizedCommands
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "rpgoalinfo";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_playerManager.TryGetSessionByUsername(args[0], out var player))
        {
            shell.WriteError(Loc.GetString("cmd-ghostkick-player-not-found"));
            return;
        }

        if (!_mind.TryGetMind(player, out var mindId, out var mind))
        {
            shell.WriteLine($"{player.Name}: no mind found.");
            return;
        }

        if (string.IsNullOrWhiteSpace(mind.RPGoalLocaleKey))
        {
            shell.WriteLine($"{player.Name}: no RP goal selected.");
            return;
        }

        shell.WriteLine($"{player.Name}: {Loc.GetString(mind.RPGoalLocaleKey)} ({mind.RPGoalId ?? "unknown"}) on {mindId}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                "Player name");
        }

        return CompletionResult.Empty;
    }
}
