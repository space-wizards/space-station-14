using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Preferences;
using Content.Server.Utility;
using Content.Shared;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Network;

#nullable enable

namespace Content.Server.Database
{
    /// <summary>
    ///     Provides methods to retrieve and update character preferences.
    ///     Don't use this directly, go through <see cref="ServerPreferencesManager" /> instead.
    /// </summary>
    public sealed class ServerDbSqlite : ServerDbBase
    {
        // For SQLite we use a single DB context via SQLite.
        // This doesn't allow concurrent access so that's what the semaphore is for.
        // That said, this is bloody SQLite, I don't even think EFCore bothers to truly async it.
        private readonly SemaphoreSlim _prefsSemaphore = new(1, 1);

        private readonly Task _dbReadyTask;
        private readonly SqliteServerDbContext _prefsCtx;

        public ServerDbSqlite(DbContextOptions<ServerDbContext> options)
        {
            _prefsCtx = new SqliteServerDbContext(options);

            if (IoCManager.Resolve<IConfigurationManager>().GetCVar(CCVars.DatabaseSynchronous))
            {
                _prefsCtx.Database.Migrate();
                _dbReadyTask = Task.CompletedTask;
            }
            else
            {
                _dbReadyTask = Task.Run(() => _prefsCtx.Database.Migrate());
            }
        }

        public override async Task<ServerBanDef?> GetServerBanAsync(IPAddress? address, NetUserId? userId)
        {
            await using var db = await GetDbImpl();

            // SQLite can't do the net masking stuff we need to match IP address ranges.
            // So just pull down the whole list into memory.
            var bans = await db.SqliteDbContext.Ban
                .Include(p => p.Unban)
                .Where(p => p.Unban == null && (p.ExpirationTime == null || p.ExpirationTime.Value > DateTime.UtcNow))
                .ToListAsync();

            foreach (var ban in bans)
            {
                if (address != null && ban.Address != null && address.IsInSubnet(ban.Address))
                {
                    return ConvertBan(ban);
                }

                if (userId is { } id && ban.UserId == id.UserId)
                {
                    return ConvertBan(ban);
                }
            }

            return null;
        }

        public override async Task<List<ServerBanDef>> GetServerBansAsync(IPAddress? address, NetUserId? userId)
        {
            await using var db = await GetDbImpl();

            // SQLite can't do the net masking stuff we need to match IP address ranges.
            // So just pull down the whole list into memory.
            var queryBans = await db.SqliteDbContext.Ban
                .Include(p => p.Unban)
                .ToListAsync();

            var bans = new List<ServerBanDef>();

            foreach (var ban in queryBans)
            {
                ServerBanDef? banDef = null;

                if (address != null && ban.Address != null && address.IsInSubnet(ban.Address))
                {
                    banDef = ConvertBan(ban);
                }
                else if (userId is { } id && ban.UserId == id.UserId)
                {
                    banDef = ConvertBan(ban);
                }

                if (banDef == null)
                {
                    continue;
                }

                bans.Add(banDef);
            }

            return bans;
        }

        public override async Task AddServerBanAsync(ServerBanDef serverBan)
        {
            await using var db = await GetDbImpl();

            string? addrStr = null;
            if (serverBan.Address is { } addr)
            {
                addrStr = $"{addr.address}/{addr.cidrMask}";
            }

            db.SqliteDbContext.Ban.Add(new SqliteServerBan
            {
                Address = addrStr,
                Reason = serverBan.Reason,
                BanningAdmin = serverBan.BanningAdmin?.UserId,
                BanTime = serverBan.BanTime.UtcDateTime,
                ExpirationTime = serverBan.ExpirationTime?.UtcDateTime,
                UserId = serverBan.UserId?.UserId
            });

            await db.SqliteDbContext.SaveChangesAsync();
        }

        public override async Task UpdatePlayerRecord(NetUserId userId, string userName, IPAddress address)
        {
            await using var db = await GetDbImpl();

            var record = await db.SqliteDbContext.Player.SingleOrDefaultAsync(p => p.UserId == userId.UserId);
            if (record == null)
            {
                db.SqliteDbContext.Player.Add(record = new SqlitePlayer
                {
                    FirstSeenTime = DateTime.UtcNow,
                    UserId = userId.UserId,
                });
            }

            record.LastSeenTime = DateTime.UtcNow;
            record.LastSeenAddress = address.ToString();
            record.LastSeenUserName = userName;

            await db.SqliteDbContext.SaveChangesAsync();
        }

        public override async Task<PlayerRecord?> GetPlayerRecordByUserName(string userName, CancellationToken cancel)
        {
            await using var db = await GetDbImpl();

            // Sort by descending last seen time.
            // So if due to account renames we have two people with the same username in the DB,
            // the most recent one is picked.
            var record = await db.SqliteDbContext.Player
                .OrderByDescending(p => p.LastSeenTime)
                .FirstOrDefaultAsync(p => p.LastSeenUserName == userName, cancel);

            return MakePlayerRecord(record);
        }

        public override async Task<PlayerRecord?> GetPlayerRecordByUserId(NetUserId userId, CancellationToken cancel)
        {
            await using var db = await GetDbImpl();

            var record = await db.SqliteDbContext.Player
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId, cancel);

            return MakePlayerRecord(record);
        }

        private static PlayerRecord? MakePlayerRecord(SqlitePlayer? record)
        {
            if (record == null)
            {
                return null;
            }

            return new PlayerRecord(
                new NetUserId(record.UserId),
                new DateTimeOffset(record.FirstSeenTime, TimeSpan.Zero),
                record.LastSeenUserName,
                new DateTimeOffset(record.LastSeenTime, TimeSpan.Zero),
                IPAddress.Parse(record.LastSeenAddress));
        }
        private static ServerBanDef? ConvertBan(SqliteServerBan? ban)
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

            (IPAddress, int)? addrTuple = null;
            if (ban.Address != null)
            {
                var idx = ban.Address.IndexOf('/', StringComparison.Ordinal);
                addrTuple = (IPAddress.Parse(ban.Address.AsSpan(0, idx)),
                    int.Parse(ban.Address.AsSpan(idx + 1), provider: CultureInfo.InvariantCulture));
            }

            return new ServerBanDef(
                ban.Id,
                uid,
                addrTuple,
                ban.BanTime,
                ban.ExpirationTime,
                ban.Reason,
                aUid);
        }

        public override async Task AddConnectionLogAsync(NetUserId userId, string userName, IPAddress address)
        {
            await using var db = await GetDbImpl();

            db.SqliteDbContext.ConnectionLog.Add(new SqliteConnectionLog
            {
                Address = address.ToString(),
                Time = DateTime.UtcNow,
                UserId = userId.UserId,
                UserName = userName
            });

            await db.SqliteDbContext.SaveChangesAsync();
        }

        public override async Task<((Admin, string? lastUserName)[] admins, AdminRank[])> GetAllAdminAndRanksAsync(
            CancellationToken cancel)
        {
            await using var db = await GetDbImpl();

            var admins = await db.SqliteDbContext.Admin
                .Include(a => a.Flags)
                .GroupJoin(db.SqliteDbContext.Player, a => a.UserId, p => p.UserId, (a, grouping) => new {a, grouping})
                .SelectMany(t => t.grouping.DefaultIfEmpty(), (t, p) => new {t.a, p!.LastSeenUserName})
                .ToArrayAsync(cancel);

            var adminRanks = await db.DbContext.AdminRank.Include(a => a.Flags).ToArrayAsync(cancel);

            return (admins.Select(p => (p.a, p.LastSeenUserName)).ToArray(), adminRanks)!;
        }

        private async Task<DbGuardImpl> GetDbImpl()
        {
            await _dbReadyTask;
            await _prefsSemaphore.WaitAsync();

            return new DbGuardImpl(this);
        }

        protected override async Task<DbGuard> GetDb()
        {
            return await GetDbImpl();
        }

        private sealed class DbGuardImpl : DbGuard
        {
            private readonly ServerDbSqlite _db;

            public DbGuardImpl(ServerDbSqlite db)
            {
                _db = db;
            }

            public override ServerDbContext DbContext => _db._prefsCtx;
            public SqliteServerDbContext SqliteDbContext => _db._prefsCtx;

            public override ValueTask DisposeAsync()
            {
                _db._prefsSemaphore.Release();
                return default;
            }
        }
    }
}
