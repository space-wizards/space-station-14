using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed class ReadyAllCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override string Command => "readyall";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var ready = true;

        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            shell.WriteError(Loc.GetString("shell-can-only-run-from-pre-round-lobby"));
            return;
        }

        if (args.Length > 0 && !bool.TryParse(args[0], out ready))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-boolean"));
            return;
        }

        _gameTicker.ToggleReadyAll(ready);
    }
}
