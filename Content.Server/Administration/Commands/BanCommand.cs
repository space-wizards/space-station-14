using System;
using Content.Server.Database;
using Content.Server.Database.Entity.Models;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;
using Robust.Shared.Network;

#nullable enable

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Ban)]
    public sealed class BanCommand : IClientCommand
    {
        public string Command => "ban";
        public string Description => "Bans somebody";
        public string Help => "Usage: <name or user ID> <reason> <duration in minutes, or 0 for permanent ban>";

        public async void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            var dbMan = IoCManager.Resolve<IServerDbManager>();

            var target = args[0];
            var reason = args[1];
            var duration = int.Parse(args[2]);
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
                shell.SendText(player, "Unable to find user with that name.");
                return;
            }

            DateTime banTime = DateTime.UtcNow;

            var ban = new ServerBan {
                UserId = targetUid,
                Address = null,
                BanTime = banTime,
                ExpirationTime = duration > 0 ? banTime + TimeSpan.FromMinutes(duration) : null,
                Reason = reason,
                BanningAdmin = player?.UserId
            };
            await dbMan.AddServerBanAsync(ban);

            if (plyMgr.TryGetSessionById(targetUid, out var targetPlayer))
            {
                var expiresDescrption = "This is a permanent ban.";
                if (ban.ExpirationTime is {} expireTime)
                {
                    var durationTime = expireTime - ban.BanTime;
                    expiresDescrption = $"This ban is for {durationTime.TotalMinutes:N0} minutes and will expire at {expireTime:f} UTC.";
                }
                targetPlayer.ConnectedClient.Disconnect(
                    $@"You, or another user of this computer or connection, are banned from playing here." +
                    $"The ban reason is: \"{ban.Reason}\"\n{expiresDescrption}"
                );
            }
            shell.SendText(player, "Ban added.");
        }
    }
}
