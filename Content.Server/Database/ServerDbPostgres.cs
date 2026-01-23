using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.IP;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Database
{
    public sealed partial class ServerDbPostgres : ServerDbBase
    {
        private readonly DbContextOptions<PostgresServerDbContext> _options;
        private readonly ISawmill _notifyLog;
        private readonly SemaphoreSlim _prefsSemaphore;
        private readonly Task _dbReadyTask;

        private int _msLag;

        public ServerDbPostgres(DbContextOptions<PostgresServerDbContext> options,
            string connectionString,
            IConfigurationManager cfg,
            ISawmill opsLog,
            ISawmill notifyLog)
            : base(opsLog)
        {
            var concurrency = cfg.GetCVar(CCVars.DatabasePgConcurrency);

            _options = options;
            _notifyLog = notifyLog;
            _prefsSemaphore = new SemaphoreSlim(concurrency, concurrency);

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

            cfg.OnValueChanged(CCVars.DatabasePgFakeLag, v => _msLag = v, true);

            InitNotificationListener(connectionString);
        }

        #region Ban
        public override async Task<BanDef?> GetBanAsync(int id)
        {
            await using var db = await GetDbImpl();

            var query = db.PgDbContext.Ban
                .ApplyIncludes(GetBanDefIncludes())
                .Where(p => p.Id == id)
                .AsSplitQuery();

            var ban = await query.SingleOrDefaultAsync();

            return ConvertBan(ban);
        }

        public override async Task<BanDef?> GetBanAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds,
            BanType type)
        {
            if (address == null && userId == null && hwId == null)
            {
                throw new ArgumentException("Address, userId, and hwId cannot all be null");
            }

            await using var db = await GetDbImpl();

            var exempt = await GetBanExemptionCore(db, userId);
            var newPlayer = userId == null || !await PlayerRecordExists(db, userId.Value);
            var query = MakeBanLookupQuery(address, userId, hwId, modernHWIds, db, includeUnbanned: false, exempt, newPlayer, type)
                .OrderByDescending(b => b.BanTime);

            var ban = await query.FirstOrDefaultAsync();

            return ConvertBan(ban);
        }

        public override async Task<List<BanDef>> GetBansAsync(IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds,
            bool includeUnbanned,
            BanType type)
        {
            if (address == null && userId == null && hwId == null)
            {
                throw new ArgumentException("Address, userId, and hwId cannot all be null");
            }

            await using var db = await GetDbImpl();

            var exempt = type == BanType.Role ? null : await GetBanExemptionCore(db, userId);
            var newPlayer = !await db.PgDbContext.Player.AnyAsync(p => p.UserId == userId);
            var query = MakeBanLookupQuery(address, userId, hwId, modernHWIds, db, includeUnbanned, exempt, newPlayer, type);
            var queryBans = await query.ToArrayAsync();
            var bans = new List<BanDef>(queryBans.Length);

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

        // This has to return IDs instead of direct objects because otherwise all the includes are too complicated.
        private static IQueryable<Ban> MakeBanLookupQuery(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            ImmutableArray<ImmutableArray<byte>>? modernHWIds,
            DbGuardImpl db,
            bool includeUnbanned,
            ServerBanExemptFlags? exemptFlags,
            bool newPlayer,
            BanType type)
        {
            DebugTools.Assert(!(address == null && userId == null && hwId == null));

            var selectorQueries = new List<IQueryable<IBanSelector>>();

            if (userId is { } uid)
                selectorQueries.Add(db.DbContext.BanPlayer.Where(b => b.UserId == uid.UserId));

            if (hwId != null && hwId.Value.Length > 0)
            {
                selectorQueries.Add(db.DbContext.BanHwid.Where(bh =>
                    bh.HWId!.Type == HwidType.Legacy && bh.HWId!.Hwid.SequenceEqual(hwId.Value.ToArray())
                ));
            }

            if (modernHWIds != null)
            {
                foreach (var modernHwid in modernHWIds)
                {
                    selectorQueries.Add(db.DbContext.BanHwid
                        .Where(b => b.HWId!.Type == HwidType.Modern
                                    && b.HWId!.Hwid.SequenceEqual(modernHwid.ToArray())));
                }
            }

            if (address != null && !exemptFlags.GetValueOrDefault(ServerBanExemptFlags.None)
                    .HasFlag(ServerBanExemptFlags.IP))
            {
                selectorQueries.Add(db.PgDbContext.BanAddress
                    .Where(ba => EF.Functions.ContainsOrEqual(ba.Address, address)
                                 && !(ba.Ban!.ExemptFlags.HasFlag(ServerBanExemptFlags.BlacklistedRange) &&
                                      !newPlayer)));
            }

            DebugTools.Assert(
                selectorQueries.Count > 0,
                "At least one filter item (IP/UserID/HWID) must have been given to make query not null.");

            var selectorQuery = selectorQueries
                .Select(q => q.Select(sel => sel.BanId))
                .Aggregate((selectors, queryable) => selectors.Union(queryable));

            var banQuery = db.DbContext.Ban.Where(b => selectorQuery.Contains(b.Id));

            if (!includeUnbanned)
            {
                banQuery = banQuery.Where(p =>
                    p.Unban == null && (p.ExpirationTime == null || p.ExpirationTime.Value > DateTime.UtcNow));
            }

            if (exemptFlags is { } exempt)
            {
                if (exempt != ServerBanExemptFlags.None)
                    exempt |= ServerBanExemptFlags.BlacklistedRange; // Any kind of exemption should bypass BlacklistedRange

                banQuery = banQuery.Where(b => (b.ExemptFlags & exempt) == 0);
            }

            return banQuery
                .Where(b => b.Type == type)
                .ApplyIncludes(GetBanDefIncludes(type))
                .AsSplitQuery();
        }

        [return: NotNullIfNotNull(nameof(ban))]
        private static BanDef? ConvertBan(Ban? ban)
        {
            if (ban == null)
            {
                return null;
            }

            NetUserId? aUid = null;
            if (ban.BanningAdmin is {} aGuid)
            {
                aUid = new NetUserId(aGuid);
            }

            var unbanDef = ConvertUnban(ban.Unban);

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
                ban.BanTime,
                ban.ExpirationTime,
                [..ban.Rounds!.Select(r => r.RoundId)],
                ban.PlaytimeAtNote,
                ban.Reason,
                ban.Severity,
                aUid,
                unbanDef,
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
            if (unban.UnbanningAdmin is {} aGuid)
            {
                aUid = new NetUserId(aGuid);
            }

            return new UnbanDef(
                unban.Id,
                aUid,
                unban.UnbanTime);
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
            db.PgDbContext.Ban.Add(banEntity);

            await db.PgDbContext.SaveChangesAsync();
            return ConvertBan(banEntity);
        }

        public override async Task AddUnbanAsync(UnbanDef unban)
        {
            await using var db = await GetDbImpl();

            db.PgDbContext.Unban.Add(new Unban
            {
                BanId = unban.BanId,
                UnbanningAdmin = unban.UnbanningAdmin?.UserId,
                UnbanTime = unban.UnbanTime.UtcDateTime
            });

            await db.PgDbContext.SaveChangesAsync();
        }
        #endregion

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

            db.PgDbContext.ConnectionLog.Add(connectionLog);

            await db.PgDbContext.SaveChangesAsync();

            return connectionLog.Id;
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

        protected override IQueryable<AdminLog> StartAdminLogsQuery(ServerDbContext db, LogFilter? filter = null)
        {
            // https://learn.microsoft.com/en-us/ef/core/querying/sql-queries#passing-parameters
            // Read the link above for parameterization before changing this method or you get the bullet
            if (!string.IsNullOrWhiteSpace(filter?.Search))
            {
                return db.AdminLog.FromSql($"""
SELECT a.admin_log_id, a.round_id, a.date, a.impact, a.json, a.message, a.type FROM admin_log AS a
WHERE to_tsvector('english'::regconfig, a.message) @@ websearch_to_tsquery('english'::regconfig, {filter.Search})
""");
            }

            return db.AdminLog;
        }

        protected override DateTime NormalizeDatabaseTime(DateTime time)
        {
            DebugTools.Assert(time.Kind == DateTimeKind.Utc);
            return time;
        }

        private async Task<DbGuardImpl> GetDbImpl(
            CancellationToken cancel = default,
            [CallerMemberName] string? name = null)
        {
            LogDbOp(name);

            await _dbReadyTask;
            await _prefsSemaphore.WaitAsync(cancel);

            if (_msLag > 0)
                await Task.Delay(_msLag, cancel);

            return new DbGuardImpl(this, new PostgresServerDbContext(_options));
        }

        protected override async Task<DbGuard> GetDb(
            CancellationToken cancel = default,
            [CallerMemberName] string? name = null)
        {
            return await GetDbImpl(cancel, name);
        }

        private sealed class DbGuardImpl : DbGuard
        {
            private readonly ServerDbPostgres _db;

            public DbGuardImpl(ServerDbPostgres db, PostgresServerDbContext dbC)
            {
                _db = db;
                PgDbContext = dbC;
            }

            public PostgresServerDbContext PgDbContext { get; }
            public override ServerDbContext DbContext => PgDbContext;

            public override async ValueTask DisposeAsync()
            {
                await DbContext.DisposeAsync();
                _db._prefsSemaphore.Release();
            }
        }
    }
}
