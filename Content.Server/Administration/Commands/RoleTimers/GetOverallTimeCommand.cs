using Content.Server.Roles;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.RoleTimers;

[AdminCommand(AdminFlags.Admin)]
public sealed class GetOverallTimeCommand : IConsoleCommand
{
    public string Command => "getoveralltime";
    public string Description => "Gets the specified minutes for a player's overall playtime";
    public string Help => $"Usage: {Command} <netuserid>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine("Name a player to get the role timer information from");
            return;
        }

        if (!IoCManager.Resolve<IPlayerManager>().TryGetUserId(args[0], out var userId))
        {
            shell.WriteError($"Did not find userid for {args[0]}");
            return;
        }

        var roles = IoCManager.Resolve<RoleTimerManager>();
        var timers = roles.GetOverallPlaytime(userId).Result;
        shell.WriteLine($"Overall time for {userId} is {timers.TotalMinutes:0} minutes");
    }
}
