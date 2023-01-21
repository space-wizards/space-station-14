using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;

namespace Content.Server.Database
{
    public sealed class ServerDbPostgres : ServerDbBase
    {
        private readonly DbContextOptions<PostgresServerDbContext> _options;
        private readonly Task _dbReadyTask;

        public ServerDbPostgres(DbContextOptions<PostgresServerDbContext> options)
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

            var query = MakeBanLookupQuery(address, userId, hwId, db, includeUnbanned: false)
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

            var query = MakeBanLookupQuery(address, userId, hwId, db, includeUnbanned);

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
            bool includeUnbanned)
        {
            IQueryable<ServerBan>? query = null;

            if (userId is { } uid)
            {
                var newQ = db.PgDbContext.Ban
                    .Include(p => p.Unban)
                    .Where(b => b.UserId == uid.UserId);

                query = query == null ? newQ : query.Union(newQ);
            }

            if (address != null)
            {
                var newQ = db.PgDbContext.Ban
                    .Include(p => p.Unban)
                    .Where(b => b.Address != null && EF.Functions.ContainsOrEqual(b.Address.Value, address));

                query = query == null ? newQ : query.Union(newQ);
            }

            if (hwId != null && hwId.Value.Length > 0)
            {
                var newQ = db.PgDbContext.Ban
                    .Include(p => p.Unban)
                    .Where(b => b.HWId!.SequenceEqual(hwId.Value.ToArray()));

                query = query == null ? newQ : query.Union(newQ);
            }

            if (!includeUnbanned)
            {
                query = query?.Where(p =>
                    p.Unban == null && (p.ExpirationTime == null || p.ExpirationTime.Value > DateTime.Now));
            }

            query = query!.Distinct();
            return query;
        }

        private static ServerBanDef? ConvertBan(ServerBan? ban)
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
                ban.HWId == null ? null : ImmutableArray.Create(ban.HWId),
                ban.BanTime,
                ban.ExpirationTime,
                ban.Reason,
                aUid,
                unbanDef);
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
                Address = serverBan.Address,
                HWId = serverBan.HWId?.ToArray(),
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
                    .Where(b => b.UserId == uid.UserId);

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
                    p.Unban == null && (p.ExpirationTime == null || p.ExpirationTime.Value > DateTime.Now));
            }

            query = query!.Distinct();
            return query;
        }

        private static ServerRoleBanDef? ConvertRoleBan(ServerRoleBan? ban)
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

            var unbanDef = ConvertRoleUnban(ban.Unban);

            return new ServerRoleBanDef(
                ban.Id,
                uid,
                ban.Address,
                ban.HWId == null ? null : ImmutableArray.Create(ban.HWId),
                ban.BanTime,
                ban.ExpirationTime,
                ban.Reason,
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

        public override async Task AddServerRoleBanAsync(ServerRoleBanDef serverRoleBan)
        {
            await using var db = await GetDbImpl();

            db.PgDbContext.RoleBan.Add(new ServerRoleBan
            {
                Address = serverRoleBan.Address,
                HWId = serverRoleBan.HWId?.ToArray(),
                Reason = serverRoleBan.Reason,
                BanningAdmin = serverRoleBan.BanningAdmin?.UserId,
                BanTime = serverRoleBan.BanTime.UtcDateTime,
                ExpirationTime = serverRoleBan.ExpirationTime?.UtcDateTime,
                UserId = serverRoleBan.UserId?.UserId,
                RoleId = serverRoleBan.Role,
            });

            await db.PgDbContext.SaveChangesAsync();
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

        protected override PlayerRecord MakePlayerRecord(Player record)
        {
            return new PlayerRecord(
                new NetUserId(record.UserId),
                new DateTimeOffset(record.FirstSeenTime),
                record.LastSeenUserName,
                new DateTimeOffset(record.LastSeenTime),
                record.LastSeenAddress,
                record.LastSeenHWId?.ToImmutableArray());
        }

        public override async Task<int> AddConnectionLogAsync(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableArray<byte> hwId,
            ConnectionDenyReason? denied)
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
