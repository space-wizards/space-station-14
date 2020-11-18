using System;
using Content.Server.Database;
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

            DateTimeOffset? expires = null;
            if (duration > 0)
            {
                expires = DateTimeOffset.Now + TimeSpan.FromMinutes(duration);
            }

            await dbMan.AddServerBanAsync(new ServerBanDef(targetUid, null, DateTimeOffset.Now, expires, reason, player?.UserId));

            if (plyMgr.TryGetSessionById(targetUid, out var targetPlayer))
            {
                targetPlayer.ConnectedClient.Disconnect("You've been banned. Tough shit.");
            }
        }
    }
}
