using System.Net;
using System.Net.Sockets;
using System.Text;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class RoleBanCommand : IConsoleCommand
{
    public string Command => "roleban";
    public string Description => "Bans a player from a role";
    public string Help => $"Usage: {Command} <name or user ID> <role> <reason> [duration in minutes, leave out or 0 for permanent ban]";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player as IPlayerSession;
        var locator = IoCManager.Resolve<IPlayerLocator>();

        string target;
        string role;
        string reason;
        uint minutes;

        switch (args.Length)
        {
            case 3:
                target = args[0];
                role = args[1];
                reason = args[2];
                minutes = 0;
                break;
            case 4:
                target = args[0];
                role = args[1];
                reason = args[2];

                if (!uint.TryParse(args[3], out minutes))
                {
                    shell.WriteLine($"{args[3]} is not a valid amount of minutes.\n{Help}");
                    return;
                }

                break;
            default:
                shell.WriteLine($"Invalid amount of arguments.");
                shell.WriteLine(Help);
                return;
        }

        var located = await locator.LookupIdByNameOrIdAsync(target);
        if (located == null)
        {
            shell.WriteError("Unable to find a player with that name.");
            return;
        }

        var targetUid = located.UserId;
        var targetHWid = located.LastHWId;
        var targetAddress = located.LastAddress;

        DateTimeOffset? expires = null;
        if (minutes > 0)
        {
            expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes);
        }

        (IPAddress, int)? addressRange = null;
        if (targetAddress != null)
        {
            if (targetAddress.IsIPv4MappedToIPv6)
                targetAddress = targetAddress.MapToIPv4();

            // Ban /64 for IPv4, /32 for IPv4.
            var cidr = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 64 : 32;
            addressRange = (targetAddress, cidr);
        }

        var banDef = new ServerRoleBanDef(
            null,
            targetUid,
            addressRange,
            targetHWid,
            DateTimeOffset.Now,
            expires,
            reason,
            player?.UserId,
            null,
            role);

        if (!await EntitySystem.Get<RoleBanSystem>().AddRoleBan(banDef))
        {
            shell.WriteLine($"{target} already has a role ban for {role}");
            return;
        }

        var response = new StringBuilder($"Role banned {target} with reason \"{reason}\"");

        response.Append(expires == null ?
            " permanently."
            : $" until {expires}");

        shell.WriteLine(response.ToString());
    }
}
