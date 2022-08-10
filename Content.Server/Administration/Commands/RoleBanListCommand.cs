using System.Linq;
using System.Text;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class RoleBanListCommand : IConsoleCommand
{
    public string Command => "rolebanlist";
    public string Description => Loc.GetString("cmd-rolebanlist-desc");
    public string Help => Loc.GetString("cmd-rolebanlist-help");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 && args.Length != 2)
        {
            shell.WriteLine($"Invalid amount of args. {Help}");
            return;
        }

        var includeUnbanned = true;
        if (args.Length == 2 && !bool.TryParse(args[1], out includeUnbanned))
        {
            shell.WriteLine($"Argument two ({args[1]}) is not a boolean.");
            return;
        }

        var dbMan = IoCManager.Resolve<IServerDbManager>();

        var target = args[0];

        var locator = IoCManager.Resolve<IPlayerLocator>();
        var located = await locator.LookupIdByNameOrIdAsync(target);
        if (located == null)
        {
            shell.WriteError("Unable to find a player with that name or id.");
            return;
        }

        var targetUid = located.UserId;
        var targetHWid = located.LastHWId;
        var targetAddress = located.LastAddress;

        var bans = await dbMan.GetServerRoleBansAsync(targetAddress, targetUid, targetHWid, includeUnbanned);

        if (bans.Count == 0)
        {
            shell.WriteLine("That user has no bans in their record.");
            return;
        }

        var bansString = new StringBuilder("Bans in record:\n");

        var first = true;
        foreach (var ban in bans)
        {
            if (!first)
                bansString.Append("\n\n");
            else
                first = false;

            bansString
                .Append("Ban ID: ")
                .Append(ban.Id)
                .Append('\n')
                .Append("Role: ")
                .Append(ban.Role)
                .Append('\n')
                .Append("Banned on ")
                .Append(ban.BanTime);

            if (ban.ExpirationTime != null)
            {
                bansString
                    .Append(" until ")
                    .Append(ban.ExpirationTime.Value);
            }

            bansString
                .Append('\n');

            bansString
                .Append("Reason: ")
                .Append(ban.Reason);
        }

        shell.WriteLine(bansString.ToString());
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString("cmd-rolebanlist-hint-1")),
            2 => CompletionResult.FromHintOptions(CompletionHelper.Booleans,
                Loc.GetString("cmd-rolebanlist-hint-2")),
            _ => CompletionResult.Empty
        };
    }
}
