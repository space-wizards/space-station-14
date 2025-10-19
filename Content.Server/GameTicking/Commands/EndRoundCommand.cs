using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed class EndRoundCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override string Command => "endround";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound)
        {
            shell.WriteLine(Loc.GetString("shell-can-only-run-while-round-is-active"));
            return;
        }

        _gameTicker.EndRound();
    }
}
