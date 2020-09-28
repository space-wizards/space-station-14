using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;

#nullable enable

namespace Content.Server.Database
{
    public sealed class ServerDbPostgres : ServerDbBase
    {
        private readonly DbContextOptions<ServerDbContext> _options;
        private readonly Task _dbReadyTask;

        public ServerDbPostgres(DbContextOptions<ServerDbContext> options)
        {
            _options = options;

            _dbReadyTask = Task.Run(async () =>
            {
                await using var ctx = new PostgresServerDbContext(_options);
                try
                {
                    await ctx.Database.MigrateAsync();
                }
                finally
                {
                    await ctx.DisposeAsync();
                }
            });
        }

        public override async Task<ServerBanDef?> GetServerBanAsync(IPAddress? address, NetUserId? userId)
        {
            if (address == null && userId == null)
            {
                throw new ArgumentException("Address and userId cannot both be null");
            }

            await using var db = await GetDbImpl();

            var query = db.PgDbContext.Ban
                .Include(p => p.Unban)
                .Where(p => p.Unban == null && (p.ExpirationTime == null || p.ExpirationTime.Value > DateTime.Now));

            if (userId is { } uid)
            {
                if (address == null)
                {
                    // Only have a user ID.
                    query = query.Where(p => p.UserId == uid.UserId);
                }
                else
                {
                    // Have both user ID and IP address.
                    query = query.Where(p =>
                        (p.Address != null && EF.Functions.ContainsOrEqual(p.Address.Value, address))
                        || p.UserId == uid.UserId);
                }
            }
            else
            {
                // Only have a connecting address.
                query = query.Where(
                    p => p.Address != null && EF.Functions.ContainsOrEqual(p.Address.Value, address));
            }

            var ban = await query.FirstOrDefaultAsync();

            return ConvertBan(ban);
        }

        private static ServerBanDef? ConvertBan(PostgresServerBan? ban)
        {
            if (ban == null)
            {
                return null;
            }

            NetUserId? uid = null;
            if (ban.UserId is {} guid)
            {
                uid = new NetUserId(guid);
            }

            NetUserId? aUid = null;
            if (ban.BanningAdmin is {} aGuid)
            {
                aUid = new NetUserId(aGuid);
            }

            return new ServerBanDef(
                uid,
                ban.Address,
                ban.BanTime,
                ban.ExpirationTime,
                ban.Reason,
                aUid);
        }

        public override async Task AddServerBanAsync(ServerBanDef serverBan)
        {
            await using var db = await GetDbImpl();

            db.PgDbContext.Ban.Add(new PostgresServerBan
            {
                Address = serverBan.Address,
                Reason = serverBan.Reason,
                BanningAdmin = serverBan.BanningAdmin?.UserId,
                BanTime = serverBan.BanTime.UtcDateTime,
                ExpirationTime = serverBan.ExpirationTime?.UtcDateTime,
                UserId = serverBan.UserId?.UserId
            });

            await db.PgDbContext.SaveChangesAsync();
        }

        public override async Task UpdatePlayerRecord(NetUserId userId, string userName, IPAddress address)
        {
            await using var db = await GetDbImpl();

            var record = await db.PgDbContext.Player.SingleOrDefaultAsync(p => p.UserId == userId.UserId);
            if (record == null)
            {
                db.PgDbContext.Player.Add(record = new PostgresPlayer
                {
                    FirstSeenTime = DateTime.UtcNow,
                    UserId = userId.UserId,
                });
            }

            record.LastSeenTime = DateTime.UtcNow;
            record.LastSeenAddress = address;
            record.LastSeenUserName = userName;

            await db.PgDbContext.SaveChangesAsync();
        }

        public override async Task AddConnectionLogAsync(NetUserId userId, string userName, IPAddress address)
        {
            await using var db = await GetDbImpl();

            db.PgDbContext.ConnectionLog.Add(new PostgresConnectionLog
            {
                Address = address,
                Time = DateTime.UtcNow,
                UserId = userId.UserId,
                UserName = userName
            });

            await db.PgDbContext.SaveChangesAsync();
        }

        private async Task<DbGuardImpl> GetDbImpl()
        {
            await _dbReadyTask;

            return new DbGuardImpl(new PostgresServerDbContext(_options));
        }

        protected override async Task<DbGuard> GetDb()
        {
            return await GetDbImpl();
        }

        private sealed class DbGuardImpl : DbGuard
        {
            public DbGuardImpl(PostgresServerDbContext dbC)
            {
                PgDbContext = dbC;
            }

            public PostgresServerDbContext PgDbContext { get; }
            public override ServerDbContext DbContext => PgDbContext;

            public override ValueTask DisposeAsync()
            {
                return DbContext.DisposeAsync();
            }
        }
    }
}
