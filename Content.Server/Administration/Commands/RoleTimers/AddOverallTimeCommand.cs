using Content.Server.Roles;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.RoleTimers;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddOverallTimeCommand : IConsoleCommand
{
    public string Command => "addoveralltime";
    public string Description => "Adds the specified minutes to a player's overall playtime";
    public string Help => $"Usage: {Command} <netuserid> <minutes>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine("Name a player to get the role timer information from");
            return;
        }

        if (!int.TryParse(args[1], out var minutes))
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
        roles.AddTimeToOverallPlaytime(userId, TimeSpan.FromSeconds(minutes));
        var timers = roles.GetOverallPlaytime(userId).Result;
        shell.WriteLine($"Increased overall time for {args[0]} to {timers.TotalMinutes:0}");
    }
}
