using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed class ReadyAll : LocalizedEntityCommands
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override string Command => "readyall";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var ready = true;

        if (args.Length > 0)
            ready = bool.Parse(args[0]);

        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            shell.WriteError(Loc.GetString("shell-command-only-available-in-lobby"));
            return;
        }

        _gameTicker.ToggleReadyAll(ready);
    }
}
