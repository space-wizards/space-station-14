using Content.Server.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.RoleTimers;

public sealed class AddRoleTimeCommand : IConsoleCommand
{
    public string Command => "addroletime";
    public string Description => "Adds the specified minutes to a player's role playtime";
    public string Help => $"Usage: {Command} <netuserid> <role> <minutes>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteLine("Name a player to get the role timer information from");
            return;
        }

        if (!int.TryParse(args[2], out var minutes))
        {
            shell.WriteError($"Unable to parse {args[1]} as minutes");
            return;
        }

        if (!IoCManager.Resolve<IPlayerManager>().TryGetUserId(args[0], out var userId))
        {
            shell.WriteError($"Did not find userid for {args[0]}");
            return;
        }

        var roles = IoCManager.Resolve<RoleTimerManager>();
        roles.AddTimeToRole(userId, args[1], TimeSpan.FromSeconds(minutes));
        var timers = roles.GetOverallPlaytime(userId).Result;
        shell.WriteLine($"Increased role playtime for {args[0]} / \'{args[1]}\' to {timers.TotalMinutes:0}");
    }
}
