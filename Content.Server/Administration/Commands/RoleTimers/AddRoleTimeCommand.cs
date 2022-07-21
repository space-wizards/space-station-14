using Content.Server.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.RoleTimers;

public sealed class AddRoleTimeCommand : IConsoleCommand
{
    public string Command => "addroletime";
    public string Description => Loc.GetString("add-role-time-desc");
    public string Help => Loc.GetString("add-role-time-help", ("command", $"{Command}"));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteLine(Loc.GetString("add-role-time-help-plain"));
            return;
        }

        if (!int.TryParse(args[2], out var minutes))
        {
            shell.WriteError(Loc.GetString("parse-minutes-fail", ("minutes", args[2])));
            return;
        }

        if (!IoCManager.Resolve<IPlayerManager>().TryGetUserId(args[0], out var userId))
        {
            shell.WriteError(Loc.GetString("parse-userid-fail", ("userid", args[0])));
            return;
        }

        var roles = IoCManager.Resolve<RoleTimerManager>();
        roles.AddTimeToRole(userId, args[1], TimeSpan.FromMinutes(minutes));
        var timers = roles.GetOverallPlaytime(userId).Result;
        shell.WriteLine(Loc.GetString("add-role-time-succeed",
            ("userid", args[0]),
            ("role", args[1]),
            ("time", $"{timers.TotalMinutes:0}")));
    }
}
