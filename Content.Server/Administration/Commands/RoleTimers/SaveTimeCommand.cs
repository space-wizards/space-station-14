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
    public string Description => "Saves the player's playtimes to the db";
    public string Help => $"Usage: {Command} <netuserid>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine("Name a player to get the role timer information from");
            return;
        }

        var pManager = IoCManager.Resolve<IPlayerManager>();

        if (!pManager.TryGetUserId(args[0], out var userId))
        {
            shell.WriteError($"Did not find userid for {args[0]}");
            return;
        }

        if (!pManager.TryGetSessionById(userId, out var pSession))
        {
            shell.WriteError($"Did not find session for {userId}");
            return;
        }

        var roles = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<RoleTimerSystem>();
        roles.Save(pSession, IoCManager.Resolve<IGameTiming>().CurTime);
        shell.WriteLine($"Saved playtime for {userId}");
    }
}
