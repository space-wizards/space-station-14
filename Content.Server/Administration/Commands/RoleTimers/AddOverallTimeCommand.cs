using Content.Server.Roles;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.RoleTimers;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddOverallTimeCommand : IConsoleCommand
{
    public string Command => "addoveralltime";
    public string Description => Loc.GetString("add-overall-time-desc");
    public string Help => Loc.GetString("add-overall-time-help", ("command", $"{Command}"));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Loc.GetString("add-overall-time-help-plain"));
            return;
        }

        if (!int.TryParse(args[1], out var minutes))
        {
            shell.WriteError(Loc.GetString("add-overall-time-parse", ("minutes", args[1])));
            return;
        }

        if (!IoCManager.Resolve<IPlayerManager>().TryGetUserId(args[0], out var userId))
        {
            shell.WriteError(Loc.GetString("add-overall-time-userid", ("userid", args[0])));
            return;
        }

        var roles = IoCManager.Resolve<RoleTimerManager>();
        roles.AddTimeToOverallPlaytime(userId, TimeSpan.FromMinutes(minutes));
        var timers = roles.GetOverallPlaytime(userId).Result;
        shell.WriteLine(Loc.GetString("add-overall-time-succeed", ("username", args[0]), ("time", $"{timers.TotalMinutes:0}")));
    }
}
