using Content.Server.Roles;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.RoleTimers;

[AdminCommand(AdminFlags.Admin)]
public sealed class GetOverallTimeCommand : IConsoleCommand
{
    public string Command => "getoveralltime";
    public string Description => Loc.GetString("get-overall-time-desc");
    public string Help => Loc.GetString("get-overall-time-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("get-overall-time-help-plain"));
            return;
        }

        if (!IoCManager.Resolve<IPlayerManager>().TryGetUserId(args[0], out var userId))
        {
            shell.WriteError(Loc.GetString("parser-userid-fail", ("userid", args[0])));
            return;
        }

        var roles = IoCManager.Resolve<RoleTimerManager>();
        var timers = roles.GetOverallPlaytime(userId).Result;
        shell.WriteLine(Loc.GetString("get-overall-time-success", ("userid", userId), ("time", $"{timers.TotalMinutes:0}")));
    }
}
