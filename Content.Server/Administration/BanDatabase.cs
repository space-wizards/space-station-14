using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.Database.Bans;
using Content.Server.Interfaces;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Content.Server.Administration
{
    public class IpBanCommand : IClientCommand
    {
        public string Command => "ipban";
        public string Description => "Bans an IP address.";
        public string Help => "ipban";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length < 2)
            {
                shell.SendText(player, "Usage: ipban [ip address] [reason]\nExample: ipban 1.2.3.4 Griefer");
                return;
            }

            if (!IPAddress.TryParse(args[0], out var address))
            {
                shell.SendText(player, $"{args[0]} is not a valid IP address.");
                return;
            }

            var reason = new StringBuilder().AppendJoin(' ', args.AsEnumerable().Skip(1)).ToString();

            var banDb = IoCManager.Resolve<IBanDatabase>();
            if (banDb.GetIpBan(address) is null)
            {
                banDb.BanIpAddress(address, reason);
                shell.SendText(player, $"{address} is now banned.");
            }
            else
            {
                shell.SendText(player, $"{address} is already banned.");
            }
        }
    }

    public class IpUnbanCommand : IClientCommand
    {
        public string Command => "ipunban";
        public string Description => "Unbans an IP address.";
        public string Help => "ipunban";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Usage: ipunban [ip address]\nExample: ipunban 1.2.3.4");
                return;
            }

            if (!IPAddress.TryParse(args[0], out var address))
            {
                shell.SendText(player, $"{args[0]} is not a valid IP address.");
                return;
            }

            var banDb = IoCManager.Resolve<IBanDatabase>();
            if (banDb.GetIpBan(address) is null)
            {
                shell.SendText(player, $"{address} is not banned.");
            }
            else
            {
                banDb.UnbanIpAddress(address);
                shell.SendText(player, $"{address} is now unbanned.");
            }
        }
    }

    public class BanDatabase : IBanDatabase
    {
        private Task _dbLoadTask;
        private BansDbContext _bansCtx;

#pragma warning disable 649
        [Dependency] private readonly IServerNetManager _netManager;
        [Dependency] private readonly IDatabaseManager _dbManager;
#pragma warning restore 649
        public void StartInit()
        {
            _netManager.JudgeConnection += endpoint => GetIpBan(endpoint.Address);

            _dbLoadTask = Task.Run(() => InitDatabase(_dbManager.DbConfig));
        }

        private void InitDatabase(IDatabaseConfiguration dbConfig)
        {
            _bansCtx = dbConfig switch
            {
                SqliteConfiguration sqlite => new SqliteBansDbContext(
                    sqlite.MakeOptions<BansDbContext>()),
                PostgresConfiguration postgres => new PostgresBansDbContext(
                    postgres.MakeOptions<BansDbContext>()),
                _ => throw new NotImplementedException()
            };
            _bansCtx.Database.Migrate();
        }

        public void FinishInit()
        {
            _dbLoadTask.Wait();
        }

        [CanBeNull]
        public string GetIpBan(IPAddress address)
        {
            return GetIpBanFromDb(address)?.Reason;
        }

        public void BanIpAddress(IPAddress address, string reason)
        {
            _bansCtx.IPBans.Add(new IPBan
            {
                IpAddress = address.ToString(),
                Reason = reason
            });
            _bansCtx.SaveChanges();
        }

        public void UnbanIpAddress(IPAddress address)
        {
            var ban = GetIpBanFromDb(address);
            Debug.Assert(!(ban is null));
            _bansCtx.IPBans.Remove(ban);
            _bansCtx.SaveChanges();
        }

        [CanBeNull]
        private IPBan GetIpBanFromDb(IPAddress address)
        {
            return _bansCtx
                .IPBans
                .FirstOrDefault(ban =>ban.IpAddress == address.ToString());
        }
    }
}
