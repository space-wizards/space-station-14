using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.IP;
using Content.Server.Preferences.Managers;
using Content.Shared.CCVar;
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
        // For SQLite we use a single DB context via SQLite.
        // This doesn't allow concurrent access so that's what the semaphore is for.
        // That said, this is bloody SQLite, I don't even think EFCore bothers to truly async it.
        private readonly SemaphoreSlim _prefsSemaphore = new(1, 1);

        private readonly Task _dbReadyTask;
        private readonly SqliteServerDbContext _prefsCtx;

        private int _msDelay;

        public ServerDbSqlite(DbContextOptions<SqliteServerDbContext> options)
        {
            _prefsCtx = new SqliteServerDbContext(options);

            var cfg = IoCManager.Resolve<IConfigurationManager>();
            if (cfg.GetCVar(CCVars.DatabaseSynchronous))
            {
                _prefsCtx.Database.Migrate();
                _dbReadyTask = Task.CompletedTask;
            }
            else
            {
                _dbReadyTask = Task.Run(() => _prefsCtx.Database.Migrate());
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
            ImmutableArray<byte>? hwId)
        {
            await using var db = await GetDbImpl();

            // SQLite can't do the net masking stuff we need to match IP address ranges.
            // So just pull down the whole list into memory.
            var bans = await GetAllBans(db.SqliteDbContext, includeUnbanned: false);

            return bans.FirstOrDefault(b => BanMatches(b, address, userId, hwId)) is { } foundBan
                ? ConvertBan(foundBan)
                : null;
        }

        public override async Task<List<ServerBanDef>> GetServerBansAsync(IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId, bool includeUnbanned)
        {
            await using var db = await GetDbImpl();

            // SQLite can't do the net masking stuff we need to match IP address ranges.
            // So just pull down the whole list into memory.
            var queryBans = await GetAllBans(db.SqliteDbContext, includeUnbanned);

            return queryBans
                .Where(b => BanMatches(b, address, userId, hwId))
                .Select(ConvertBan)
                .ToList()!;
        }

        private static async Task<List<ServerBan>> GetAllBans(
            SqliteServerDbContext db,
            bool includeUnbanned)
        {
            IQueryable<ServerBan> query = db.Ban.Include(p => p.Unban);
            if (!includeUnbanned)
            {
                query = query.Where(p =>
                    p.Unban == null && (p.ExpirationTime == null || p.ExpirationTime.Value > DateTime.UtcNow));
            }

            return await query.ToListAsync();
        }

        private static bool BanMatches(
            ServerBan ban,
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId)
        {
            if (address != null && ban.Address is not null && IPAddressExt.IsInSubnet(address, ban.Address.Value))
            {
                return true;
            }

            if (userId is { } id && ban.UserId == id.UserId)
            {
                return true;
            }

            if (hwId is { } hwIdVar && hwIdVar.Length > 0 && hwIdVar.AsSpan().SequenceEqual(ban.HWId))
            {
                return true;
            }

            return false;
        }

        public override async Task AddServerBanAsync(ServerBanDef serverBan)
        {
            await using var db = await GetDbImpl();

            db.SqliteDbContext.Ban.Add(new ServerBan
            {
                Address = serverBan.Address,
                Reason = serverBan.Reason,
                BanningAdmin = serverBan.BanningAdmin?.UserId,
                HWId = serverBan.HWId?.ToArray(),
                BanTime = serverBan.BanTime.UtcDateTime,
                ExpirationTime = serverBan.ExpirationTime?.UtcDateTime,
                UserId = serverBan.UserId?.UserId
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

        public override async Task<List<ServerRoleBanDef>> GetServerRoleBansAsync(IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            bool includeUnbanned)
        {
            await using var db = await GetDbImpl();

            // SQLite can't do the net masking stuff we need to match IP address ranges.
            // So just pull down the whole list into memory.
            var queryBans = await GetAllRoleBans(db.SqliteDbContext, includeUnbanned);

            return queryBans
                .Where(b => RoleBanMatches(b, address, userId, hwId))
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
            ImmutableArray<byte>? hwId)
        {
            if (address != null && ban.Address is not null && IPAddressExt.IsInSubnet(address, ban.Address.Value))
            {
                return true;
            }

            if (userId is { } id && ban.UserId == id.UserId)
            {
                return true;
            }

            if (hwId is { } hwIdVar && hwIdVar.Length > 0 && hwIdVar.AsSpan().SequenceEqual(ban.HWId))
            {
                return true;
            }

            return false;
        }

        public override async Task AddServerRoleBanAsync(ServerRoleBanDef serverBan)
        {
            await using var db = await GetDbImpl();

            db.SqliteDbContext.RoleBan.Add(new ServerRoleBan
            {
                Address = serverBan.Address,
                Reason = serverBan.Reason,
                BanningAdmin = serverBan.BanningAdmin?.UserId,
                HWId = serverBan.HWId?.ToArray(),
                BanTime = serverBan.BanTime.UtcDateTime,
                ExpirationTime = serverBan.ExpirationTime?.UtcDateTime,
                UserId = serverBan.UserId?.UserId,
                RoleId = serverBan.Role,
            });

            await db.SqliteDbContext.SaveChangesAsync();
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

        private static ServerRoleBanDef? ConvertRoleBan(ServerRoleBan? ban)
        {
            if (ban == null)
            {
                return null;
            }

            NetUserId? uid = null;
            if (ban.UserId is { } guid)
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
                ban.Address,
                ban.HWId == null ? null : ImmutableArray.Create(ban.HWId),
                ban.BanTime,
                ban.ExpirationTime,
                ban.Reason,
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
                unban.UnbanTime);
        }
        #endregion

        protected override PlayerRecord MakePlayerRecord(Player record)
        {
            return new PlayerRecord(
                new NetUserId(record.UserId),
                new DateTimeOffset(record.FirstSeenTime, TimeSpan.Zero),
                record.LastSeenUserName,
                new DateTimeOffset(record.LastSeenTime, TimeSpan.Zero),
                record.LastSeenAddress,
                record.LastSeenHWId?.ToImmutableArray());
        }

        private static ServerBanDef? ConvertBan(ServerBan? ban)
        {
            if (ban == null)
            {
                return null;
            }

            NetUserId? uid = null;
            if (ban.UserId is { } guid)
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
                ban.Address,
                ban.HWId == null ? null : ImmutableArray.Create(ban.HWId),
                ban.BanTime,
                ban.ExpirationTime,
                ban.Reason,
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
                unban.UnbanTime);
        }

        public override async Task<int>  AddConnectionLogAsync(
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
                Denied = denied
            };

            db.SqliteDbContext.ConnectionLog.Add(connectionLog);

            await db.SqliteDbContext.SaveChangesAsync();

            return connectionLog.Id;
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

        public override async Task<int> AddNewRound(Server server, params Guid[] playerIds)
        {
            await using var db = await GetDb();

            var players = await db.DbContext.Player
                .Where(player => playerIds.Contains(player.UserId))
                .ToListAsync();

            var nextId = 1;
            if (await db.DbContext.Round.AnyAsync())
            {
                nextId = db.DbContext.Round.Max(round => round.Id) + 1;
            }

            var round = new Round
            {
                Id = nextId,
                Players = players,
                ServerId = server.Id
            };

            db.DbContext.Round.Add(round);

            await db.DbContext.SaveChangesAsync();

            return round.Id;
        }

        public override async Task AddAdminLogs(List<QueuedLog> logs)
        {
            await using var db = await GetDb();

            var nextId = 1;
            if (await db.DbContext.AdminLog.AnyAsync())
            {
                nextId = db.DbContext.AdminLog.Max(round => round.Id) + 1;
            }

            var entities = new Dictionary<int, AdminLogEntity>();

            foreach (var (log, entityData) in logs)
            {
                log.Id = nextId++;

                var logEntities = new List<AdminLogEntity>(entityData.Count);
                foreach (var (id, name) in entityData)
                {
                    var entity = entities.GetOrNew(id);
                    entity.Name = name;
                    logEntities.Add(entity);
                }

                foreach (var player in log.Players)
                {
                    player.LogId = log.Id;
                }

                log.Entities = logEntities;
                db.DbContext.AdminLog.Add(log);
            }

            await db.DbContext.SaveChangesAsync();
        }

        public override async Task<int> AddAdminNote(AdminNote note)
        {
            await using (var db = await GetDb())
            {
                var nextId = 1;
                if (await db.DbContext.AdminNotes.AnyAsync())
                {
                    nextId = await db.DbContext.AdminNotes.MaxAsync(dbVersion => dbVersion.Id) + 1;
                }

                note.Id = nextId;
            }

            return await base.AddAdminNote(note);
        }

        private async Task<DbGuardImpl> GetDbImpl()
        {
            await _dbReadyTask;
            if (_msDelay > 0)
                await Task.Delay(_msDelay);

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
