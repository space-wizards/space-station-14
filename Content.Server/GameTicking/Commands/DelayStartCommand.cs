using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed partial class DelayStartCommand : LocalizedEntityCommands
{
    [Dependency] private GameTicker _gameTicker = default!;

    public override string Command => "delaystart";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            shell.WriteLine(Loc.GetString("cmd-delaystart-preround-only"));
            return;
        }

        if (args.Length == 0)
        {
            var paused = _gameTicker.TogglePause();
            shell.WriteLine(paused ? Loc.GetString("cmd-delaystart-paused") : Loc.GetString("cmd-delaystart-resumed"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-need-between-arguments", ("lower", 0), ("upper", 1)));
            return;
        }

        if (!int.TryParse(args[0], out var seconds) || seconds == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-delaystart-invalid-seconds", ("seconds", args[0])));
            return;
        }

        var time = TimeSpan.FromSeconds(seconds);
        if (!_gameTicker.DelayStart(time))
        {
            shell.WriteLine(Loc.GetString("shell-unknown-error"));
        }
    }
}
