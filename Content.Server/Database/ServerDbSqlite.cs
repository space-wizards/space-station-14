using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Preferences;
using Content.Server.Utility;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;
using Robust.Shared.Utility;

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
        private readonly SemaphoreSlim _prefsSemaphore = new SemaphoreSlim(1, 1);

        private readonly Task _dbReadyTask;
        private readonly SqliteServerDbContext _prefsCtx;

        public ServerDbSqlite(DbContextOptions<ServerDbContext> options)
        {
            _prefsCtx = new SqliteServerDbContext(options);

            _dbReadyTask = Task.Run(() => _prefsCtx.Database.Migrate());
        }

        public override async Task<ServerBanDef?> GetServerBanAsync(IPAddress? address, NetUserId? userId)
        {
            await using var db = await GetDbImpl();

            // SQLite can't do the net masking stuff we need to match IP address ranges.
            // So just pull down the whole list into memory.
            var bans = await db.SqliteDbContext.Bans
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

        public override async Task AddServerBanAsync(ServerBanDef serverBan)
        {
            await using var db = await GetDbImpl();

            string? addrStr = null;
            if (serverBan.Address is { } addr)
            {
                addrStr = $"{addr.address}/{addr.cidrMask}";
            }

            db.SqliteDbContext.Bans.Add(new SqliteServerBan
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
                uid,
                addrTuple,
                ban.BanTime,
                ban.ExpirationTime,
                ban.Reason,
                aUid);
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
