using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.VarEdit)]
public sealed class ThrowScoreboardCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _e = default!;

    public string Command => "throwscoreboard";

    public string Description => Loc.GetString("throw-scoreboard-command-description");

    public string Help => Loc.GetString("throw-scoreboard-command-help-text");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 0)
        {
            shell.WriteLine(Help);
            return;
        }
        // DS14-start
        // This command may only rebroadcast the frozen snapshot, never execute end-round side effects.
        if (!_e.System<GameTicker>().RebroadcastRoundEndScoreboard())
            shell.WriteError("Round-end snapshot is not available yet.");
        // DS14-end
    }
}
