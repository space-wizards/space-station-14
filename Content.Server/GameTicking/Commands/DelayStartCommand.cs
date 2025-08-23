using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands;

[AdminCommand(AdminFlags.Round)]
sealed class DelayStartCommand : LocalizedEntityCommands
{
    public override string Command => "adjustroundstart";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var ticker = EntityManager.System<GameTicker>();
        if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            shell.WriteLine(Loc.GetString("adjustroundstart-preround-only"));
            return;
        }

        if (args.Length == 0)
        {
            var paused = ticker.TogglePause();
            shell.WriteLine(paused ? Loc.GetString("adjustroundstart-paused") : Loc.GetString("adjustroundstart-resumed"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-need-between-arguments", ("lower", 0), ("upper", 1)));
            return;
        }

        if (!int.TryParse(args[0], out var seconds) || seconds == 0)
        {
            shell.WriteLine(Loc.GetString("adjustroundstart-invalid-seconds", ("seconds", args[0])));
            return;
        }

        var time = TimeSpan.FromSeconds(seconds);
        if (!ticker.DelayStart(time))
        {
            shell.WriteLine(Loc.GetString("shell-unknown-error"));
        }
    }
}
