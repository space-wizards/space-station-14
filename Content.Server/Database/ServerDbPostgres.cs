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
        public override async Task<ServerBanDef?> GetServerBanAsync(int id)
        {
            await using var db = await GetDbImpl();

            var query = db.PgDbContext.Ban
                .Include(p => p.Unban)
                .Where(p => p.Id == id);

            var ban = await query.SingleOrDefaultAsync();

            return ConvertBan(ban);
        }

        public override async Task<ServerBanDef?> GetServerBanAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId)
        {
            if (address == null && userId == null && hwId == null)
            {
                throw new ArgumentException("Address, userId, and hwId cannot all be null");
            }

            await using var db = await GetDbImpl();

            var exempt = await GetBanExemptionCore(db, userId);
            var newPlayer = userId == null || !await PlayerRecordExists(db, userId.Value);
            var query = MakeBanLookupQuery(address, userId, hwId, db, includeUnbanned: false, exempt, newPlayer)
                .OrderByDescending(b => b.BanTime);

            var ban = await query.FirstOrDefaultAsync();

            return ConvertBan(ban);
        }

        public override async Task<List<ServerBanDef>> GetServerBansAsync(IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId, bool includeUnbanned)
        {
            if (address == null && userId == null && hwId == null)
            {
                throw new ArgumentException("Address, userId, and hwId cannot all be null");
            }

            await using var db = await GetDbImpl();

            var exempt = await GetBanExemptionCore(db, userId);
            var newPlayer = !await db.PgDbContext.Player.AnyAsync(p => p.UserId == userId);
            var query = MakeBanLookupQuery(address, userId, hwId, db, includeUnbanned, exempt, newPlayer);

            var queryBans = await query.ToArrayAsync();
            var bans = new List<ServerBanDef>(queryBans.Length);

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

        private static IQueryable<ServerBan> MakeBanLookupQuery(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            DbGuardImpl db,
            bool includeUnbanned,
            ServerBanExemptFlags? exemptFlags,
            bool newPlayer)
        {
            DebugTools.Assert(!(address == null && userId == null && hwId == null));

            IQueryable<ServerBan>? query = null;

            if (userId is { } uid)
            {
                var newQ = db.PgDbContext.Ban
                    .Include(p => p.Unban)
                    .Where(b => b.PlayerUserId == uid.UserId);

                query = query == null ? newQ : query.Union(newQ);
            }

            if (address != null && !exemptFlags.GetValueOrDefault(ServerBanExemptFlags.None).HasFlag(ServerBanExemptFlags.IP))
            {
                var newQ = db.PgDbContext.Ban
                    .Include(p => p.Unban)
                    .Where(b => b.Address != null
                                && EF.Functions.ContainsOrEqual(b.Address.Value, address)
                                && !(b.ExemptFlags.HasFlag(ServerBanExemptFlags.BlacklistedRange) && !newPlayer));

                query = query == null ? newQ : query.Union(newQ);
            }

            if (hwId != null && hwId.Value.Length > 0)
            {
                var newQ = db.PgDbContext.Ban
                    .Include(p => p.Unban)
                    .Where(b => b.HWId!.SequenceEqual(hwId.Value.ToArray()));

                query = query == null ? newQ : query.Union(newQ);
            }

            DebugTools.Assert(
                query != null,
                "At least one filter item (IP/UserID/HWID) must have been given to make query not null.");

            if (!includeUnbanned)
            {
                query = query.Where(p =>
                    p.Unban == null && (p.ExpirationTime == null || p.ExpirationTime.Value > DateTime.UtcNow));
            }

            if (exemptFlags is { } exempt)
            {
                if (exempt != ServerBanExemptFlags.None)
                    exempt |= ServerBanExemptFlags.BlacklistedRange; // Any kind of exemption should bypass BlacklistedRange

                query = query.Where(b => (b.ExemptFlags & exempt) == 0);
            }

            return query.Distinct();
        }

        private static ServerBanDef? ConvertBan(ServerBan? ban)
        {
            if (ban == null)
            {
                return null;
            }

            NetUserId? uid = null;
            if (ban.PlayerUserId is {} guid)
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
                ban.Address.ToTuple(),
                ban.HWId == null ? null : ImmutableArray.Create(ban.HWId),
                ban.BanTime,
                ban.ExpirationTime,
                ban.RoundId,
                ban.PlaytimeAtNote,
                ban.Reason,
                ban.Severity,
                aUid,
                unbanDef,
                ban.ExemptFlags);
        }

        private static ServerUnbanDef? ConvertUnban(ServerUnban? unban)
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

            db.PgDbContext.Ban.Add(new ServerBan
            {
                Address = serverBan.Address.ToNpgsqlInet(),
                HWId = serverBan.HWId?.ToArray(),
                Reason = serverBan.Reason,
                Severity = serverBan.Severity,
                BanningAdmin = serverBan.BanningAdmin?.UserId,
                BanTime = serverBan.BanTime.UtcDateTime,
                ExpirationTime = serverBan.ExpirationTime?.UtcDateTime,
                RoundId = serverBan.RoundId,
                PlaytimeAtNote = serverBan.PlaytimeAtNote,
                PlayerUserId = serverBan.UserId?.UserId,
                ExemptFlags = serverBan.ExemptFlags
            });

            await db.PgDbContext.SaveChangesAsync();
        }

        public override async Task AddServerUnbanAsync(ServerUnbanDef serverUnban)
        {
            await using var db = await GetDbImpl();

            db.PgDbContext.Unban.Add(new ServerUnban
            {
                BanId = serverUnban.BanId,
                UnbanningAdmin = serverUnban.UnbanningAdmin?.UserId,
                UnbanTime = serverUnban.UnbanTime.UtcDateTime
            });

            await db.PgDbContext.SaveChangesAsync();
        }
        #endregion

        #region Role Ban
        public override async Task<ServerRoleBanDef?> GetServerRoleBanAsync(int id)
        {
            await using var db = await GetDbImpl();

            var query = db.PgDbContext.RoleBan
                .Include(p => p.Unban)
                .Where(p => p.Id == id);

            var ban = await query.SingleOrDefaultAsync();

            return ConvertRoleBan(ban);

        }

        public override async Task<List<ServerRoleBanDef>> GetServerRoleBansAsync(IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            bool includeUnbanned)
        {
            if (address == null && userId == null && hwId == null)
            {
                throw new ArgumentException("Address, userId, and hwId cannot all be null");
            }

            await using var db = await GetDbImpl();

            var query = MakeRoleBanLookupQuery(address, userId, hwId, db, includeUnbanned)
                .OrderByDescending(b => b.BanTime);

            return await QueryRoleBans(query);
        }

        private static async Task<List<ServerRoleBanDef>> QueryRoleBans(IQueryable<ServerRoleBan> query)
        {
            var queryRoleBans = await query.ToArrayAsync();
            var bans = new List<ServerRoleBanDef>(queryRoleBans.Length);

            foreach (var ban in queryRoleBans)
            {
                var banDef = ConvertRoleBan(ban);

                if (banDef != null)
                {
                    bans.Add(banDef);
                }
            }

            return bans;
        }

        private static IQueryable<ServerRoleBan> MakeRoleBanLookupQuery(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            DbGuardImpl db,
            bool includeUnbanned)
        {
            IQueryable<ServerRoleBan>? query = null;

            if (userId is { } uid)
            {
                var newQ = db.PgDbContext.RoleBan
                    .Include(p => p.Unban)
                    .Where(b => b.PlayerUserId == uid.UserId);

                query = query == null ? newQ : query.Union(newQ);
            }

            if (address != null)
            {
                var newQ = db.PgDbContext.RoleBan
                    .Include(p => p.Unban)
                    .Where(b => b.Address != null && EF.Functions.ContainsOrEqual(b.Address.Value, address));

                query = query == null ? newQ : query.Union(newQ);
            }

            if (hwId != null && hwId.Value.Length > 0)
            {
                var newQ = db.PgDbContext.RoleBan
                    .Include(p => p.Unban)
                    .Where(b => b.HWId!.SequenceEqual(hwId.Value.ToArray()));

                query = query == null ? newQ : query.Union(newQ);
            }

            if (!includeUnbanned)
            {
                query = query?.Where(p =>
                    p.Unban == null && (p.ExpirationTime == null || p.ExpirationTime.Value > DateTime.UtcNow));
            }

            query = query!.Distinct();
            return query;
        }

        [return: NotNullIfNotNull(nameof(ban))]
        private static ServerRoleBanDef? ConvertRoleBan(ServerRoleBan? ban)
        {
            if (ban == null)
            {
                return null;
            }

            NetUserId? uid = null;
            if (ban.PlayerUserId is {} guid)
            {
                uid = new NetUserId(guid);
            }

            NetUserId? aUid = null;
            if (ban.BanningAdmin is {} aGuid)
            {
                aUid = new NetUserId(aGuid);
            }

            var unbanDef = ConvertRoleUnban(ban.Unban);

            return new ServerRoleBanDef(
                ban.Id,
                uid,
                ban.Address.ToTuple(),
                ban.HWId == null ? null : ImmutableArray.Create(ban.HWId),
                ban.BanTime,
                ban.ExpirationTime,
                ban.RoundId,
                ban.PlaytimeAtNote,
                ban.Reason,
                ban.Severity,
                aUid,
                unbanDef,
                ban.RoleId);
        }

        private static ServerRoleUnbanDef? ConvertRoleUnban(ServerRoleUnban? unban)
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

            return new ServerRoleUnbanDef(
                unban.Id,
                aUid,
                unban.UnbanTime);
        }

        public override async Task<ServerRoleBanDef> AddServerRoleBanAsync(ServerRoleBanDef serverRoleBan)
        {
            await using var db = await GetDbImpl();

            var ban = new ServerRoleBan
            {
                Address = serverRoleBan.Address.ToNpgsqlInet(),
                HWId = serverRoleBan.HWId?.ToArray(),
                Reason = serverRoleBan.Reason,
                Severity = serverRoleBan.Severity,
                BanningAdmin = serverRoleBan.BanningAdmin?.UserId,
                BanTime = serverRoleBan.BanTime.UtcDateTime,
                ExpirationTime = serverRoleBan.ExpirationTime?.UtcDateTime,
                RoundId = serverRoleBan.RoundId,
                PlaytimeAtNote = serverRoleBan.PlaytimeAtNote,
                PlayerUserId = serverRoleBan.UserId?.UserId,
                RoleId = serverRoleBan.Role,
            };
            db.PgDbContext.RoleBan.Add(ban);

            await db.PgDbContext.SaveChangesAsync();
            return ConvertRoleBan(ban);
        }

        public override async Task AddServerRoleUnbanAsync(ServerRoleUnbanDef serverRoleUnban)
        {
            await using var db = await GetDbImpl();

            db.PgDbContext.RoleUnban.Add(new ServerRoleUnban
            {
                BanId = serverRoleUnban.BanId,
                UnbanningAdmin = serverRoleUnban.UnbanningAdmin?.UserId,
                UnbanTime = serverRoleUnban.UnbanTime.UtcDateTime
            });

            await db.PgDbContext.SaveChangesAsync();
        }
        #endregion

        public override async Task<int> AddConnectionLogAsync(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableArray<byte> hwId,
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
                HWId = hwId.ToArray(),
                Denied = denied,
                ServerId = serverId
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
