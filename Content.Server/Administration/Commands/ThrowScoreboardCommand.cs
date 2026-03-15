using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.VarEdit)]
public sealed class ThrowScoreboardCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override string Command => "throwscoreboard";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 0)
        {
            shell.WriteLine(Help);
            return;
        }
        _gameTicker.ShowRoundEndScoreboard();
    }
}
