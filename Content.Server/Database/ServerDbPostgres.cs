using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
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

        public override async Task<ServerBanDef?> GetServerBanAsync(int id)
        {
            await using var db = await GetDbImpl();

            var query = db.PgDbContext.Ban
                .Include(p => p.Unban)
                .Where(p => p.Id == id);

            var ban = await query.SingleOrDefaultAsync();

            return ConvertBan(ban);
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

        public override async Task<List<ServerBanDef>> GetServerBansAsync(IPAddress? address, NetUserId? userId)
        {
            if (address == null && userId == null)
            {
                throw new ArgumentException("Address and userId cannot both be null");
            }

            await using var db = await GetDbImpl();

            var query = db.PgDbContext.Ban
                .Include(p => p.Unban).AsQueryable();

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

            var queryBans = await query.ToArrayAsync();
            var bans = new List<ServerBanDef>();

            foreach (var ban in queryBans)
            {
                var banDef = ConvertBan(ban);

                if (banDef != null)
                {
                    bans.Add(banDef);
                }
            }

            return bans;
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

            var unbanDef = ConvertUnban(ban.Unban);

            return new ServerBanDef(
                ban.Id,
                uid,
                ban.Address,
                ban.BanTime,
                ban.ExpirationTime,
                ban.Reason,
                aUid,
                unbanDef);
        }

        private static ServerUnbanDef? ConvertUnban(PostgresServerUnban? unban)
        {
            if (unban == null)
            {
                return null;
            }

            NetUserId? aUid = null;
            if (unban.UnbanningAdmin is {} aGuid)
            {
                aUid = new NetUserId(aGuid);
            }

            return new ServerUnbanDef(
                unban.Id,
                aUid,
                unban.UnbanTime);
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

        public override async Task AddServerUnbanAsync(ServerUnbanDef serverUnban)
        {
            await using var db = await GetDbImpl();

            db.PgDbContext.Unban.Add(new PostgresServerUnban
            {
                 BanId = serverUnban.BanId,
                 UnbanningAdmin = serverUnban.UnbanningAdmin?.UserId,
                 UnbanTime = serverUnban.UnbanTime.UtcDateTime
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

        public override async Task<PlayerRecord?> GetPlayerRecordByUserName(string userName, CancellationToken cancel)
        {
            await using var db = await GetDbImpl();

            // Sort by descending last seen time.
            // So if, due to account renames, we have two people with the same username in the DB,
            // the most recent one is picked.
            var record = await db.PgDbContext.Player
                .OrderByDescending(p => p.LastSeenTime)
                .FirstOrDefaultAsync(p => p.LastSeenUserName == userName, cancel);

            return MakePlayerRecord(record);
        }

        public override async Task<PlayerRecord?> GetPlayerRecordByUserId(NetUserId userId, CancellationToken cancel)
        {
            await using var db = await GetDbImpl();

            var record = await db.PgDbContext.Player
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId, cancel);

            return MakePlayerRecord(record);
        }

        private static PlayerRecord? MakePlayerRecord(PostgresPlayer? record)
        {
            if (record == null)
            {
                return null;
            }

            return new PlayerRecord(
                new NetUserId(record.UserId),
                new DateTimeOffset(record.FirstSeenTime),
                record.LastSeenUserName,
                new DateTimeOffset(record.LastSeenTime),
                record.LastSeenAddress);
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

        public override async Task<((Admin, string? lastUserName)[] admins, AdminRank[])>
            GetAllAdminAndRanksAsync(CancellationToken cancel)
        {
            await using var db = await GetDbImpl();

            // Honestly this probably doesn't even matter but whatever.
            await using var tx =
                await db.DbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancel);

            // Join with the player table to find their last seen username, if they have one.
            var admins = await db.PgDbContext.Admin
                .Include(a => a.Flags)
                .GroupJoin(db.PgDbContext.Player, a => a.UserId, p => p.UserId, (a, grouping) => new {a, grouping})
                .SelectMany(t => t.grouping.DefaultIfEmpty(), (t, p) => new {t.a, p!.LastSeenUserName})
                .ToArrayAsync(cancel);

            var adminRanks = await db.DbContext.AdminRank.Include(a => a.Flags).ToArrayAsync(cancel);

            return (admins.Select(p => (p.a, p.LastSeenUserName)).ToArray(), adminRanks)!;
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
