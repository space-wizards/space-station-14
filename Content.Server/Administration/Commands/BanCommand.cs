using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;


namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Ban)]
    public sealed class BanCommand : LocalizedCommands
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public override string Command => "ban";

        public override async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            var locator = IoCManager.Resolve<IPlayerLocator>();
            var dbMan = IoCManager.Resolve<IServerDbManager>();

            string target;
            string reason;
            uint minutes;

            switch (args.Length)
            {
                case 2:
                    target = args[0];
                    reason = args[1];
                    minutes = 0;
                    break;
                case 3:
                    target = args[0];
                    reason = args[1];

                    if (!uint.TryParse(args[2], out minutes))
                    {
                        shell.WriteLine($"{args[2]} is not a valid amount of minutes.\n{Help}");
                        return;
                    }

                    break;
                default:
                    shell.WriteLine($"Invalid amount of arguments.{Help}");
                    return;
            }

            var located = await locator.LookupIdByNameOrIdAsync(target);
            if (located == null)
            {
                shell.WriteError(LocalizationManager.GetString("cmd-ban-player"));
                return;
            }

            var targetUid = located.UserId;
            var targetHWid = located.LastHWId;
            var targetAddr = located.LastAddress;

            if (player != null && player.UserId == targetUid)
            {
                shell.WriteLine(LocalizationManager.GetString("cmd-ban-self"));
                return;
            }

            DateTimeOffset? expires = null;
            if (minutes > 0)
            {
                expires = DateTimeOffset.Now + TimeSpan.FromMinutes(minutes);
            }

            (IPAddress, int)? addrRange = null;
            if (targetAddr != null)
            {
                if (targetAddr.IsIPv4MappedToIPv6)
                    targetAddr = targetAddr.MapToIPv4();

                // Ban /64 for IPv4, /32 for IPv4.
                var cidr = targetAddr.AddressFamily == AddressFamily.InterNetworkV6 ? 64 : 32;
                addrRange = (targetAddr, cidr);
            }

            var banDef = new ServerBanDef(
                null,
                targetUid,
                addrRange,
                targetHWid,
                DateTimeOffset.Now,
                expires,
                reason,
                player?.UserId,
                null);

            await dbMan.AddServerBanAsync(banDef);

            var response = new StringBuilder($"Banned {target} with reason \"{reason}\"");

            response.Append(expires == null ? " permanently." : $" until {expires}");

            shell.WriteLine(response.ToString());

            if (plyMgr.TryGetSessionById(targetUid, out var targetPlayer))
            {
                var message = banDef.FormatBanMessage(_cfg, LocalizationManager);
                targetPlayer.ConnectedClient.Disconnect(message);
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var playerMgr = IoCManager.Resolve<IPlayerManager>();
                var options = playerMgr.ServerSessions.Select(c => c.Name).OrderBy(c => c).ToArray();
                return CompletionResult.FromHintOptions(options, LocalizationManager.GetString("cmd-ban-hint"));
            }

            if (args.Length == 2)
                return CompletionResult.FromHint(LocalizationManager.GetString("cmd-ban-hint-reason"));

            if (args.Length == 3)
            {
                var durations = new CompletionOption[]
                {
                    new("0", LocalizationManager.GetString("cmd-ban-hint-duration-1")),
                    new("1440", LocalizationManager.GetString("cmd-ban-hint-duration-2")),
                    new("4320", LocalizationManager.GetString("cmd-ban-hint-duration-3")),
                    new("10080", LocalizationManager.GetString("cmd-ban-hint-duration-4")),
                    new("20160", LocalizationManager.GetString("cmd-ban-hint-duration-5")),
                    new("43800", LocalizationManager.GetString("cmd-ban-hint-duration-6")),
                };

                return CompletionResult.FromHintOptions(durations, LocalizationManager.GetString("cmd-ban-hint-duration"));
            }

            return CompletionResult.Empty;
        }
    }
}
