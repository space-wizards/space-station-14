using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.IP;
using Content.Server.Preferences.Managers;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Database
{
    /// <summary>
    ///     Provides methods to retrieve and update character preferences.
    ///     Don't use this directly, go through <see cref="ServerPreferencesManager" /> instead.
    /// </summary>
    public sealed class ServerDbSqlite : ServerDbBase
    {
        private readonly Func<DbContextOptions<SqliteServerDbContext>> _options;

        private readonly ConcurrencySemaphore _prefsSemaphore;

        private readonly Task _dbReadyTask;

        private int _msDelay;

        public ServerDbSqlite(
            Func<DbContextOptions<SqliteServerDbContext>> options,
            bool inMemory,
            IConfigurationManager cfg,
            bool synchronous,
            ISawmill opsLog)
            : base(opsLog)
        {
            _options = options;

            var prefsCtx = new SqliteServerDbContext(options());

            // When inMemory we re-use the same connection, so we can't have any concurrency.
            var concurrency = inMemory ? 1 : cfg.GetCVar(CCVars.DatabaseSqliteConcurrency);
            _prefsSemaphore = new ConcurrencySemaphore(concurrency, synchronous);

            if (synchronous)
            {
                prefsCtx.Database.Migrate();
                _dbReadyTask = Task.CompletedTask;
                prefsCtx.Dispose();
            }
            else
            {
                _dbReadyTask = Task.Run(() =>
                {
                    prefsCtx.Database.Migrate();
                    prefsCtx.Dispose();
                });
            }

            cfg.OnValueChanged(CCVars.DatabaseSqliteDelay, v => _msDelay = v, true);
        }

        #region Ban
        public override async Task<ServerBanDef?> GetServerBanAsync(int id)
        {
            await using var db = await GetDbImpl();

            var ban = await db.SqliteDbContext.Ban
                .Include(p => p.Unban)
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync();

            return ConvertBan(ban);
        }

        public override async Task<ServerBanDef?> GetServerBanAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds)
        {
            await using var db = await GetDbImpl();

            return (await GetServerBanQueryAsync(db, address, userId, hwId, modernHWIds, includeUnbanned: false)).FirstOrDefault();
        }

        public override async Task<List<ServerBanDef>> GetServerBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds,
            bool includeUnbanned)
        {
            await using var db = await GetDbImpl();

            return (await GetServerBanQueryAsync(db, address, userId, hwId, modernHWIds, includeUnbanned)).ToList();
        }

        private async Task<IEnumerable<ServerBanDef>> GetServerBanQueryAsync(
            DbGuardImpl db,
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds,
            bool includeUnbanned)
        {
            var exempt = await GetBanExemptionCore(db, userId);

            var newPlayer = !await db.SqliteDbContext.Player.AnyAsync(p => p.UserId == userId);

            // SQLite can't do the net masking stuff we need to match IP address ranges.
            // So just pull down the whole list into memory.
            var queryBans = await GetAllBans(db.SqliteDbContext, includeUnbanned, exempt);

            var playerInfo = new BanMatcher.PlayerInfo
            {
                Address = address,
                UserId = userId,
                ExemptFlags = exempt ?? default,
                HWId = hwId,
                ModernHWIds = modernHWIds,
                IsNewPlayer = newPlayer,
            };

            return queryBans
                .Select(ConvertBan)
                .Where(b => BanMatcher.BanMatches(b!, playerInfo))!;
        }

        private static async Task<List<ServerBan>> GetAllBans(
            SqliteServerDbContext db,
            bool includeUnbanned,
            ServerBanExemptFlags? exemptFlags)
        {
            IQueryable<ServerBan> query = db.Ban.Include(p => p.Unban);
            if (!includeUnbanned)
            {
                query = query.Where(p =>
                    p.Unban == null && (p.ExpirationTime == null || p.ExpirationTime.Value > DateTime.UtcNow));
            }

            if (exemptFlags is { } exempt)
            {
                // Any flag to bypass BlacklistedRange bans.
                if (exempt != ServerBanExemptFlags.None)
                    exempt |= ServerBanExemptFlags.BlacklistedRange;

                query = query.Where(b => (b.ExemptFlags & exempt) == 0);
            }

            return await query.ToListAsync();
        }

        public override async Task AddServerBanAsync(ServerBanDef serverBan)
        {
            await using var db = await GetDbImpl();

            db.SqliteDbContext.Ban.Add(new ServerBan
            {
                Address = serverBan.Address.ToNpgsqlInet(),
                Reason = serverBan.Reason,
                Severity = serverBan.Severity,
                BanningAdmin = serverBan.BanningAdmin?.UserId,
                HWId = serverBan.HWId,
                BanTime = serverBan.BanTime.UtcDateTime,
                ExpirationTime = serverBan.ExpirationTime?.UtcDateTime,
                RoundId = serverBan.RoundId,
                PlaytimeAtNote = serverBan.PlaytimeAtNote,
                PlayerUserId = serverBan.UserId?.UserId,
                ExemptFlags = serverBan.ExemptFlags
            });

            await db.SqliteDbContext.SaveChangesAsync();
        }

        public override async Task AddServerUnbanAsync(ServerUnbanDef serverUnban)
        {
            await using var db = await GetDbImpl();

            db.SqliteDbContext.Unban.Add(new ServerUnban
            {
                BanId = serverUnban.BanId,
                UnbanningAdmin = serverUnban.UnbanningAdmin?.UserId,
                UnbanTime = serverUnban.UnbanTime.UtcDateTime
            });

            await db.SqliteDbContext.SaveChangesAsync();
        }
        #endregion

        #region Role Ban
        public override async Task<ServerRoleBanDef?> GetServerRoleBanAsync(int id)
        {
            await using var db = await GetDbImpl();

            var ban = await db.SqliteDbContext.RoleBan
                .Include(p => p.Unban)
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync();

            return ConvertRoleBan(ban);
        }

        public override async Task<List<ServerRoleBanDef>> GetServerRoleBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds,
            bool includeUnbanned)
        {
            await using var db = await GetDbImpl();

            // SQLite can't do the net masking stuff we need to match IP address ranges.
            // So just pull down the whole list into memory.
            var queryBans = await GetAllRoleBans(db.SqliteDbContext, includeUnbanned);

            return queryBans
                .Where(b => RoleBanMatches(b, address, userId, hwId, modernHWIds))
                .Select(ConvertRoleBan)
                .ToList()!;
        }

        private static async Task<List<ServerRoleBan>> GetAllRoleBans(
            SqliteServerDbContext db,
            bool includeUnbanned)
        {
            IQueryable<ServerRoleBan> query = db.RoleBan.Include(p => p.Unban);
            if (!includeUnbanned)
            {
                query = query.Where(p =>
                    p.Unban == null && (p.ExpirationTime == null || p.ExpirationTime.Value > DateTime.UtcNow));
            }

            return await query.ToListAsync();
        }

        private static bool RoleBanMatches(
            ServerRoleBan ban,
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds)
        {
            if (address != null && ban.Address is not null && address.IsInSubnet(ban.Address.ToTuple().Value))
            {
                return true;
            }

            if (userId is { } id && ban.PlayerUserId == id.UserId)
            {
                return true;
            }

            switch (ban.HWId?.Type)
            {
                case HwidType.Legacy:
                    if (hwId is { Length: > 0 } hwIdVar && hwIdVar.AsSpan().SequenceEqual(ban.HWId.Hwid))
                        return true;
                    break;

                case HwidType.Modern:
                    if (modernHWIds != null)
                    {
                        foreach (var modernHWId in modernHWIds)
                        {
                            if (modernHWId.AsSpan().SequenceEqual(ban.HWId.Hwid))
                                return true;
                        }
                    }

                    break;
            }

            return false;
        }

        public override async Task<ServerRoleBanDef> AddServerRoleBanAsync(ServerRoleBanDef serverBan)
        {
            await using var db = await GetDbImpl();

            var ban = new ServerRoleBan
            {
                Address = serverBan.Address.ToNpgsqlInet(),
                Reason = serverBan.Reason,
                Severity = serverBan.Severity,
                BanningAdmin = serverBan.BanningAdmin?.UserId,
                HWId = serverBan.HWId,
                BanTime = serverBan.BanTime.UtcDateTime,
                ExpirationTime = serverBan.ExpirationTime?.UtcDateTime,
                RoundId = serverBan.RoundId,
                PlaytimeAtNote = serverBan.PlaytimeAtNote,
                PlayerUserId = serverBan.UserId?.UserId,
                RoleId = serverBan.Role,
            };
            db.SqliteDbContext.RoleBan.Add(ban);

            await db.SqliteDbContext.SaveChangesAsync();
            return ConvertRoleBan(ban);
        }

        public override async Task AddServerRoleUnbanAsync(ServerRoleUnbanDef serverUnban)
        {
            await using var db = await GetDbImpl();

            db.SqliteDbContext.RoleUnban.Add(new ServerRoleUnban
            {
                BanId = serverUnban.BanId,
                UnbanningAdmin = serverUnban.UnbanningAdmin?.UserId,
                UnbanTime = serverUnban.UnbanTime.UtcDateTime
            });

            await db.SqliteDbContext.SaveChangesAsync();
        }

        [return: NotNullIfNotNull(nameof(ban))]
        private static ServerRoleBanDef? ConvertRoleBan(ServerRoleBan? ban)
        {
            if (ban == null)
            {
                return null;
            }

            NetUserId? uid = null;
            if (ban.PlayerUserId is { } guid)
            {
                uid = new NetUserId(guid);
            }

            NetUserId? aUid = null;
            if (ban.BanningAdmin is { } aGuid)
            {
                aUid = new NetUserId(aGuid);
            }

            var unban = ConvertRoleUnban(ban.Unban);

            return new ServerRoleBanDef(
                ban.Id,
                uid,
                ban.Address.ToTuple(),
                ban.HWId,
                // SQLite apparently always reads DateTime as unspecified, but we always write as UTC.
                DateTime.SpecifyKind(ban.BanTime, DateTimeKind.Utc),
                ban.ExpirationTime == null ? null : DateTime.SpecifyKind(ban.ExpirationTime.Value, DateTimeKind.Utc),
                ban.RoundId,
                ban.PlaytimeAtNote,
                ban.Reason,
                ban.Severity,
                aUid,
                unban,
                ban.RoleId);
        }

        private static ServerRoleUnbanDef? ConvertRoleUnban(ServerRoleUnban? unban)
        {
            if (unban == null)
            {
                return null;
            }

            NetUserId? aUid = null;
            if (unban.UnbanningAdmin is { } aGuid)
            {
                aUid = new NetUserId(aGuid);
            }

            return new ServerRoleUnbanDef(
                unban.Id,
                aUid,
                // SQLite apparently always reads DateTime as unspecified, but we always write as UTC.
                DateTime.SpecifyKind(unban.UnbanTime, DateTimeKind.Utc));
        }
        #endregion

        [return: NotNullIfNotNull(nameof(ban))]
        private static ServerBanDef? ConvertBan(ServerBan? ban)
        {
            if (ban == null)
            {
                return null;
            }

            NetUserId? uid = null;
            if (ban.PlayerUserId is { } guid)
            {
                uid = new NetUserId(guid);
            }

            NetUserId? aUid = null;
            if (ban.BanningAdmin is { } aGuid)
            {
                aUid = new NetUserId(aGuid);
            }

            var unban = ConvertUnban(ban.Unban);

            return new ServerBanDef(
                ban.Id,
                uid,
                ban.Address.ToTuple(),
                ban.HWId,
                // SQLite apparently always reads DateTime as unspecified, but we always write as UTC.
                DateTime.SpecifyKind(ban.BanTime, DateTimeKind.Utc),
                ban.ExpirationTime == null ? null : DateTime.SpecifyKind(ban.ExpirationTime.Value, DateTimeKind.Utc),
                ban.RoundId,
                ban.PlaytimeAtNote,
                ban.Reason,
                ban.Severity,
                aUid,
                unban);
        }

        private static ServerUnbanDef? ConvertUnban(ServerUnban? unban)
        {
            if (unban == null)
            {
                return null;
            }

            NetUserId? aUid = null;
            if (unban.UnbanningAdmin is { } aGuid)
            {
                aUid = new NetUserId(aGuid);
            }

            return new ServerUnbanDef(
                unban.Id,
                aUid,
                // SQLite apparently always reads DateTime as unspecified, but we always write as UTC.
                DateTime.SpecifyKind(unban.UnbanTime, DateTimeKind.Utc));
        }

        public override async Task<int> AddConnectionLogAsync(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableTypedHwid? hwId,
            float trust,
            ConnectionDenyReason? denied,
            int serverId)
        {
            await using var db = await GetDbImpl();

            var connectionLog = new ConnectionLog
            {
                Address = address,
                Time = DateTime.UtcNow,
                UserId = userId.UserId,
                UserName = userName,
                HWId = hwId,
                Denied = denied,
                ServerId = serverId,
                Trust = trust,
            };

            db.SqliteDbContext.ConnectionLog.Add(connectionLog);

            await db.SqliteDbContext.SaveChangesAsync();

            return connectionLog.Id;
        }

        public override async Task<((Admin, string? lastUserName)[] admins, AdminRank[])> GetAllAdminAndRanksAsync(
            CancellationToken cancel)
        {
            await using var db = await GetDbImpl(cancel);

            var admins = await db.SqliteDbContext.Admin
                .Include(a => a.Flags)
                .GroupJoin(db.SqliteDbContext.Player, a => a.UserId, p => p.UserId, (a, grouping) => new {a, grouping})
                .SelectMany(t => t.grouping.DefaultIfEmpty(), (t, p) => new {t.a, p!.LastSeenUserName})
                .ToArrayAsync(cancel);

            var adminRanks = await db.DbContext.AdminRank.Include(a => a.Flags).ToArrayAsync(cancel);

            return (admins.Select(p => (p.a, p.LastSeenUserName)).ToArray(), adminRanks)!;
        }

        protected override IQueryable<AdminLog> StartAdminLogsQuery(ServerDbContext db, LogFilter? filter = null)
        {
            IQueryable<AdminLog> query = db.AdminLog;
            if (filter?.Search != null)
                query = query.Where(log => EF.Functions.Like(log.Message, $"%{filter.Search}%"));

            return query;
        }

        public override async Task<int> AddAdminNote(AdminNote note)
        {
            await using (var db = await GetDb())
            {
                var nextId = 1;
                if (await db.DbContext.AdminNotes.AnyAsync())
                {
                    nextId = await db.DbContext.AdminNotes.MaxAsync(adminNote => adminNote.Id) + 1;
                }

                note.Id = nextId;
            }

            return await base.AddAdminNote(note);
        }
        public override async Task<int> AddAdminWatchlist(AdminWatchlist watchlist)
        {
            await using (var db = await GetDb())
            {
                var nextId = 1;
                if (await db.DbContext.AdminWatchlists.AnyAsync())
                {
                    nextId = await db.DbContext.AdminWatchlists.MaxAsync(adminWatchlist => adminWatchlist.Id) + 1;
                }

                watchlist.Id = nextId;
            }

            return await base.AddAdminWatchlist(watchlist);
        }

        public override async Task<int> AddAdminMessage(AdminMessage message)
        {
            await using (var db = await GetDb())
            {
                var nextId = 1;
                if (await db.DbContext.AdminMessages.AnyAsync())
                {
                    nextId = await db.DbContext.AdminMessages.MaxAsync(adminMessage => adminMessage.Id) + 1;
                }

                message.Id = nextId;
            }

            return await base.AddAdminMessage(message);
        }

        public override Task SendNotification(DatabaseNotification notification)
        {
            // Notifications not implemented on SQLite.
            return Task.CompletedTask;
        }

        protected override DateTime NormalizeDatabaseTime(DateTime time)
        {
            DebugTools.Assert(time.Kind == DateTimeKind.Unspecified);
            return DateTime.SpecifyKind(time, DateTimeKind.Utc);
        }

        private async Task<DbGuardImpl> GetDbImpl(
            CancellationToken cancel = default,
            [CallerMemberName] string? name = null)
        {
            LogDbOp(name);
            await _dbReadyTask;
            if (_msDelay > 0)
                await Task.Delay(_msDelay, cancel);

            await _prefsSemaphore.WaitAsync(cancel);

            var dbContext = new SqliteServerDbContext(_options());

            return new DbGuardImpl(this, dbContext);
        }

        protected override async Task<DbGuard> GetDb(
            CancellationToken cancel = default,
            [CallerMemberName] string? name = null)
        {
            return await GetDbImpl(cancel, name).ConfigureAwait(false);
        }

        private sealed class DbGuardImpl : DbGuard
        {
            private readonly ServerDbSqlite _db;
            private readonly SqliteServerDbContext _ctx;

            public DbGuardImpl(ServerDbSqlite db, SqliteServerDbContext dbContext)
            {
                _db = db;
                _ctx = dbContext;
            }

            public override ServerDbContext DbContext => _ctx;
            public SqliteServerDbContext SqliteDbContext => _ctx;

            public override async ValueTask DisposeAsync()
            {
                await _ctx.DisposeAsync();
                _db._prefsSemaphore.Release();
            }
        }

        private sealed class ConcurrencySemaphore
        {
            private readonly bool _synchronous;
            private readonly SemaphoreSlim _semaphore;
            private Thread? _holdingThread;

            public ConcurrencySemaphore(int maxCount, bool synchronous)
            {
                if (synchronous && maxCount != 1)
                    throw new ArgumentException("If synchronous, max concurrency must be 1");

                _synchronous = synchronous;
                _semaphore = new SemaphoreSlim(maxCount, maxCount);
            }

            public Task WaitAsync(CancellationToken cancel = default)
            {
                var task = _semaphore.WaitAsync(cancel);

                if (_synchronous)
                {
                    if (!task.IsCompleted)
                    {
                        if (Thread.CurrentThread == _holdingThread)
                        {
                            throw new InvalidOperationException(
                                "Multiple database requests from same thread on synchronous database!");
                        }

                        throw new InvalidOperationException(
                            $"Different threads trying to access the database at once! " +
                            $"Holding thread: {DiagThread(_holdingThread)}, " +
                            $"current thread: {DiagThread(Thread.CurrentThread)}");
                    }

                    _holdingThread = Thread.CurrentThread;
                }

                return task;
            }

            public void Release()
            {
                if (_synchronous)
                {
                    if (Thread.CurrentThread != _holdingThread)
                        throw new InvalidOperationException("Released on different thread than took lock???");

                    _holdingThread = null;
                }

                _semaphore.Release();
            }

            private static string DiagThread(Thread? thread)
            {
                if (thread != null)
                    return $"{thread.Name} ({thread.ManagedThreadId})";

                return "<null thread>";
            }
        }
    }
}
