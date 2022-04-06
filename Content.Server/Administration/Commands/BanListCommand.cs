using System;
using System.Text;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Ban)]
    public sealed class BanListCommand : IConsoleCommand
    {
        public string Command => "banlist";
        public string Description => "Lists somebody's bans";
        public string Help => "Usage: <name or user ID>";

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine($"Invalid amount of args. {Help}");
                return;
            }

            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            var dbMan = IoCManager.Resolve<IServerDbManager>();

            var target = args[0];
            NetUserId targetUid;

            if (plyMgr.TryGetSessionByUsername(target, out var targetSession))
            {
                targetUid = targetSession.UserId;
            }
            else if (Guid.TryParse(target, out var targetGuid))
            {
                targetUid = new NetUserId(targetGuid);
            }
            else
            {
                shell.WriteLine("Unable to find user with that name.");
                return;
            }

            var bans = await dbMan.GetServerBansAsync(null, targetUid, null);

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
                    .Append("\n\n");
            }

            shell.WriteLine(bansString.ToString());
        }
    }
}
