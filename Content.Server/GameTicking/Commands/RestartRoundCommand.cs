using Content.Server.Administration;
using Content.Server.RoundEnd;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed partial class RestartRoundCommand : LocalizedEntityCommands
{
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private RoundEndSystem _roundEndSystem = default!;

    public override string Command => "restartround";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound)
        {
            shell.WriteLine(Loc.GetString("shell-can-only-run-while-round-is-active"));
            return;
        }

        _roundEndSystem.EndRound();
    }
}

[AdminCommand(AdminFlags.Round)]
public sealed partial class RestartRoundNowCommand : LocalizedEntityCommands
{
    [Dependency] private GameTicker _gameTicker = default!;

    public override string Command => "restartroundnow";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _gameTicker.RestartRound();
    }
}
