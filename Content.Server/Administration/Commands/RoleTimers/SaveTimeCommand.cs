using Content.Server.Roles;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Commands.RoleTimers;

/// <summary>
/// Saves the timers for a particular player immediately
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class TimeCommand : IConsoleCommand
{
    public string Command => "savetime";
    public string Description => Loc.GetString("save-time-desc");
    public string Help => Loc.GetString("save-time-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("save-time-help-plain"));
            return;
        }

        var pManager = IoCManager.Resolve<IPlayerManager>();

        if (!pManager.TryGetUserId(args[0], out var userId))
        {
            shell.WriteError(Loc.GetString("parse-userid-fail", ("userid", args[0])));
            return;
        }

        if (!pManager.TryGetSessionById(userId, out var pSession))
        {
            shell.WriteError(Loc.GetString("parse-session-fail", ("userid", userId)));
            return;
        }

        var roles = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<RoleTimerSystem>();
        roles.Save(pSession, IoCManager.Resolve<IGameTiming>().CurTime);
        shell.WriteLine(Loc.GetString("save-time-succeed", ("userid", userId)));
    }
}
