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
        public override async Task<BanDef?> GetBanAsync(int id)
        {
            await using var db = await GetDbImpl();

            var ban = await db.SqliteDbContext.Ban
                .ApplyIncludes(GetBanDefIncludes())
                .Where(p => p.Id == id)
                .AsSplitQuery()
                .SingleOrDefaultAsync();

            return ConvertBan(ban);
        }

        public override async Task<BanDef?> GetBanAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds,
            BanType type)
        {
            await using var db = await GetDbImpl();

            return (await GetBanQueryAsync(db, address, userId, hwId, modernHWIds, includeUnbanned: false, type)).FirstOrDefault();
        }

        public override async Task<List<BanDef>> GetBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds,
            bool includeUnbanned,
            BanType type)
        {
            await using var db = await GetDbImpl();

            return (await GetBanQueryAsync(db, address, userId, hwId, modernHWIds, includeUnbanned, type)).ToList();
        }

        private async Task<IEnumerable<BanDef>> GetBanQueryAsync(
            DbGuardImpl db,
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds,
            bool includeUnbanned,
            BanType type)
        {
            var exempt = await GetBanExemptionCore(db, userId);

            var newPlayer = !await db.SqliteDbContext.Player.AnyAsync(p => p.UserId == userId);

            // SQLite can't do the net masking stuff we need to match IP address ranges.
            // So just pull down the whole list into memory.
            var queryBans = await GetAllBans(db.SqliteDbContext, includeUnbanned, exempt, type);

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

        private static async Task<List<Ban>> GetAllBans(SqliteServerDbContext db,
            bool includeUnbanned,
            ServerBanExemptFlags? exemptFlags,
            BanType type)
        {
            var query = db.Ban.Where(b => b.Type == type).ApplyIncludes(GetBanDefIncludes(type));
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

            return await query.AsSplitQuery().ToListAsync();
        }

        public override async Task<BanDef> AddBanAsync(BanDef ban)
        {
            await using var db = await GetDbImpl();

            var banEntity = new Ban
            {
                Type = ban.Type,
                Addresses = [..ban.Addresses.Select(ba => new BanAddress { Address = ba.ToNpgsqlInet() })],
                Hwids = [..ban.HWIds.Select(bh => new BanHwid { HWId = bh })],
                Reason = ban.Reason,
                Severity = ban.Severity,
                BanningAdmin = ban.BanningAdmin?.UserId,
                BanTime = ban.BanTime.UtcDateTime,
                ExpirationTime = ban.ExpirationTime?.UtcDateTime,
                Rounds = [..ban.RoundIds.Select(bri => new BanRound { RoundId = bri })],
                PlaytimeAtNote = ban.PlaytimeAtNote,
                Players = [..ban.UserIds.Select(bp => new BanPlayer { UserId = bp.UserId })],
                ExemptFlags = ban.ExemptFlags,
                Roles = ban.Roles == null
                    ? []
                    : ban.Roles.Value.Select(brd => new BanRole
                        {
                            RoleType = brd.RoleType,
                            RoleId = brd.RoleId
                        })
                        .ToList(),
            };
            db.SqliteDbContext.Ban.Add(banEntity);

            await db.SqliteDbContext.SaveChangesAsync();
            return ConvertBan(banEntity);
        }

        public override async Task AddUnbanAsync(UnbanDef unban)
        {
            await using var db = await GetDbImpl();

            db.SqliteDbContext.Unban.Add(new Unban
            {
                BanId = unban.BanId,
                UnbanningAdmin = unban.UnbanningAdmin?.UserId,
                UnbanTime = unban.UnbanTime.UtcDateTime
            });

            await db.SqliteDbContext.SaveChangesAsync();
        }
        #endregion

        [return: NotNullIfNotNull(nameof(ban))]
        private static BanDef? ConvertBan(Ban? ban)
        {
            if (ban == null)
            {
                return null;
            }

            NetUserId? aUid = null;
            if (ban.BanningAdmin is { } aGuid)
            {
                aUid = new NetUserId(aGuid);
            }

            var unban = ConvertUnban(ban.Unban);

            ImmutableArray<BanRoleDef>? roles = null;
            if (ban.Type == BanType.Role)
            {
                roles = [..ban.Roles!.Select(br => new BanRoleDef(br.RoleType, br.RoleId))];
            }

            return new BanDef(
                ban.Id,
                ban.Type,
                [..ban.Players!.Select(bp => new NetUserId(bp.UserId))],
                [..ban.Addresses!.Select(ba => ba.Address.ToTuple())],
                [..ban.Hwids!.Select(bh => bh.HWId)],
                // SQLite apparently always reads DateTime as unspecified, but we always write as UTC.
                DateTime.SpecifyKind(ban.BanTime, DateTimeKind.Utc),
                ban.ExpirationTime == null ? null : DateTime.SpecifyKind(ban.ExpirationTime.Value, DateTimeKind.Utc),
                [..ban.Rounds!.Select(r => r.RoundId)],
                ban.PlaytimeAtNote,
                ban.Reason,
                ban.Severity,
                aUid,
                unban,
                ban.ExemptFlags,
                roles);
        }

        private static UnbanDef? ConvertUnban(Unban? unban)
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

            return new UnbanDef(
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
