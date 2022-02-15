using System.Text;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class ListRoleBans : IConsoleCommand
{
    public string Command => "rolebanlist";
    public string Description => "Lists the user's role bans";
    public string Help => "Usage: <name or user ID>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine($"Invalid amount of args. {Help}");
            return;
        }

        var dbMan = IoCManager.Resolve<IServerDbManager>();

        var target = args[0];

        var locator = IoCManager.Resolve<IPlayerLocator>();
        var located = await locator.LookupIdByNameOrIdAsync(target);
        if (located == null)
        {
            shell.WriteError("Unable to find a player with that name.");
            return;
        }

        var targetUid = located.UserId;
        var targetHWid = located.LastHWId;
        var targetAddress = located.LastAddress;

        var bans = await dbMan.GetServerRoleBansAsync(targetAddress, targetUid, targetHWid, true);

        if (bans.Count == 0)
        {
            shell.WriteLine("That user has no bans in their record.");
            return;
        }

        var bansString = new StringBuilder("Bans in record:\n");

        foreach (var ban in bans)
        {
            bansString
                .Append("Ban ID: ")
                .Append(ban.Id)
                .Append("\n")
                .Append("Banned in ")
                .Append(ban.BanTime);

            if (ban.ExpirationTime == null)
            {
                bansString.Append(".");
            }
            else
            {
                bansString
                    .Append(" until ")
                    .Append(ban.ExpirationTime.Value)
                    .Append(".");
            }

            bansString.Append("\n");

            bansString
                .Append("Reason: ")
                .Append(ban.Reason)
                .Append('\n');
        }

        shell.WriteLine(bansString.ToString());
    }
}
