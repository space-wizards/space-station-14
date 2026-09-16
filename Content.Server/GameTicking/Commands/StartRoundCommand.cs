using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed partial class StartRoundCommand : LocalizedEntityCommands
{
    [Dependency] private ServerGameTicker _gameTicker = default!;

    public override string Command => "startround";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            shell.WriteLine(Loc.GetString("shell-can-only-run-from-pre-round-lobby"));
            return;
        }

        _gameTicker.StartRound();
    }
}
