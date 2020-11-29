using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared;
using Content.Shared.Preferences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;
using LogLevel = Robust.Shared.Log.LogLevel;
using Content.Server.Database.Entity;
using Content.Server.Database.Entity.Models;
using Content.Server.Utility;
using System.Linq;
using System.Collections.Generic;
using Robust.Shared.Maths;
using System.Transactions;

#nullable enable

namespace Content.Server.Database
{
    public class ServerDbManager : IServerDbManager
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IResourceManager _res = default!;
        [Dependency] private readonly ILogManager _logMgr = default!;

        private ServerDbContext ServerDbContext = default!;
        private LoggingProvider _msLogProvider = default!;
        private ILoggerFactory _msLoggerFactory = default!;


        public void Init()
        {
            _msLogProvider = new LoggingProvider(_logMgr);
            _msLoggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(_msLogProvider);
            });

            ServerDbContext = CreateDbContext();

            ServerDbContext.Database.Migrate();
        }

        protected virtual ServerDbContext CreateDbContext()
        {
            var engine = _cfg.GetCVar(CCVars.DatabaseEngine).ToLower();
            switch (engine)
            {
                case "sqlite":
                    var options = CreateSqliteOptions();
                    return new SqliteServerDbContext(options);
                case "postgres":
                    options = CreatePostgresOptions();
                    return new PostgresServerDbContext(options);
                default:
                    throw new InvalidDataException("Unknown database engine {engine}.");
            }
        }

        public async Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile)
        {

            var profile = ((HumanoidCharacterProfile) defaultProfile).ConvertProfile(0);

            var prefs = new Preference
            {
                UserId = userId.UserId
            };

            ServerDbContext.Set<PreferenceProfile>()
                .Add(new PreferenceProfile {
                    Preference = prefs,
                    Profile = profile
                });

            prefs.Profiles = new List<Profile> { profile };

            ServerDbContext.Preferences.Add(prefs);

            await ServerDbContext.SaveChangesAsync();

            return new PlayerPreferences(new[] {new KeyValuePair<int, ICharacterProfile>(0, defaultProfile)}, 0);
        }

        public async Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index)
        {
            var selected = await ServerDbContext.Set<PreferenceProfile>()
                .Include(p => p.Preference)
                .Where(p => p.Preference.UserId == userId)
                .SingleOrDefaultAsync();

            var profile = await ServerDbContext.Profiles
                .Where(p => p.Slot == index && p.PreferenceId == selected.PreferenceId)
                .SingleAsync();

            ServerDbContext.Add(new PreferenceProfile {
                PreferenceId = selected.PreferenceId,
                ProfileId = profile.Id
            });

            await ServerDbContext.SaveChangesAsync();
        }

        public async Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot)
        {
            if (profile is null)
            {
                ServerDbContext.Preferences
                    .Single(p => p.UserId == userId.UserId)
                    .Profiles.ToList()
                    .RemoveAll(h => h.Slot == slot);
                await ServerDbContext.SaveChangesAsync();
                return;
            }

            if (profile is not HumanoidCharacterProfile humanoid)
            {
                // TODO: Handle other ICharacterProfile implementations properly
                throw new NotImplementedException();
            }

            Profile entity = humanoid.ConvertProfile(slot);

            var prefs = await ServerDbContext
                .Preferences
                .Include(p => p.Profiles)
                .SingleAsync(p => p.UserId == userId.UserId);

            var oldProfile = prefs
                .Profiles
                .SingleOrDefault(h => h.Slot == entity.Slot);

            if (oldProfile is not null)
            {
                prefs.Profiles.Remove(oldProfile);
            }

            prefs.Profiles.Add(entity);
            await ServerDbContext.SaveChangesAsync();
        }

        public async Task DeleteSlotAndSetSelectedIndex(NetUserId userId, int deleteSlot, int newSlot)
        {
            ServerDbContext.Preferences
                .Single(p => p.UserId == userId)
                .Profiles.ToList()
                .RemoveAll(h => h.Slot == deleteSlot); // Does that work..?
            var prefs = await ServerDbContext.Preferences
                .SingleAsync(p => p.UserId == userId.UserId);

            await SaveSelectedCharacterIndexAsync(userId, newSlot);

            await ServerDbContext.SaveChangesAsync();
        }

        public async Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId)
        {
            var prefs = await ServerDbContext
                .Preferences
                .Include(p => p.Profiles).ThenInclude(h => h.Jobs)
                .Include(p => p.Profiles).ThenInclude(h => h.Antags)
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId);

            if (prefs is null) return null;

            var selected = await ServerDbContext.Set<PreferenceProfile>()
                .Where(p => p.PreferenceId == prefs.Id)
                .Select(p => p.Profile.Slot)
                .SingleAsync();

            var maxSlot = prefs.Profiles.Max(p => p.Slot) + 1;
            var profiles = new Dictionary<int, ICharacterProfile>(maxSlot);
            foreach (var profile in prefs.Profiles)
            {
                profiles[profile.Slot] = profile.ConvertProfile();
            }

            return new PlayerPreferences(profiles, selected);
        }

        public async Task AssignUserIdAsync(string name, NetUserId userId)
        {
            ServerDbContext.AssignedUsers.Add(new AssignedUser
            {
                UserId = userId,
                UserName = name
            });

            await ServerDbContext.SaveChangesAsync();
        }

        public async Task<NetUserId?> GetAssignedUserIdAsync(string name)
        {
            var assigned = await ServerDbContext.AssignedUsers.SingleOrDefaultAsync(p => p.UserName == name);
            return (NetUserId?) assigned?.UserId;
        }

        public async Task<ServerBan?> GetServerBanAsync(IPAddress? address, NetUserId? userId)
        {
            // SQLite can't do the net masking stuff we need to match IP address ranges.
            // So just pull down the whole list of IP bans into memory too.
            var bans = await ServerDbContext.Bans
                //.Where(p => p.UserId == userId || p.UserId == null)
                .Include(p => p.Unban)
                .Where(p => p.Unban == null && (p.ExpirationTime == null || p.ExpirationTime.Value > DateTime.UtcNow))
                .ToListAsync();

            foreach (var ban in bans)
            {
                if (address is {} && ban.Address is {} && address.IsInSubnet(ban.Address.Value.Item1, ban.Address.Value.Item2))
                {
                    return ban;
                }

                if (userId is {} id && ban.UserId == id.UserId)
                {
                    return ban;
                }
            }

            return null;
        }

        public async Task AddServerBanAsync(ServerBan serverBan)
        {
            ServerDbContext.Bans.Add(serverBan);

            await ServerDbContext.SaveChangesAsync();
        }

        public async Task UpdatePlayerRecordAsync(NetUserId userId, string userName, IPAddress address)
        {
            var record = await ServerDbContext.Players.SingleOrDefaultAsync(p => p.UserId == userId.UserId);
            if (record == null)
            {
                ServerDbContext.Players.Add(record = new Player
                {
                    FirstSeenTime = DateTime.UtcNow,
                    UserId = userId.UserId,
                });
            }

            record.LastSeenTime = DateTime.UtcNow;
            record.LastSeenAddress = address;
            record.LastSeenUserName = userName;

            await ServerDbContext.SaveChangesAsync();
        }

        public async Task<Player?> GetPlayerRecordByUserName(string userName, CancellationToken cancel = default)
        {
            return await ServerDbContext.Players
                .OrderByDescending(p => p.LastSeenTime)
                .FirstOrDefaultAsync(p => p.LastSeenUserName == userName, cancel);
        }

        public async Task<Player?> GetPlayerRecordByUserId(NetUserId userId, CancellationToken cancel = default)
        {
            return await ServerDbContext.Players
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId, cancel);
        }

        public async Task AddConnectionLogAsync(NetUserId userId, string userName, IPAddress address)
        {
            ServerDbContext.ConnectionLogs.Add(new ConnectionLog
            {
                Address = address,
                Time = DateTime.UtcNow,
                UserId = userId.UserId,
                UserName = userName
            });

            await ServerDbContext.SaveChangesAsync();
        }

        public async Task<Admin?> GetAdminDataForAsync(NetUserId userId, CancellationToken cancel = default)
        {
            return await ServerDbContext.Admins
                .Include(p => p.Flags)
                .Include(p => p.AdminRank)
                .ThenInclude(p => p!.Flags)
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId, cancel);
        }

        public async Task<AdminRank?> GetAdminRankAsync(int id, CancellationToken cancel = default)
        {
            return await ServerDbContext.AdminRanks
                .Include(r => r.Flags)
                .SingleOrDefaultAsync(r => r.Id == id, cancel);
        }

        public async Task<((Admin, string? lastUserName)[] admins, AdminRank[])> GetAllAdminAndRanksAsync(
            CancellationToken cancel = default)
        {
            var admins = await ServerDbContext.Admins
                .Include(a => a.Flags)
                .GroupJoin(ServerDbContext.Players, a => a.UserId, p => p.UserId, (a, grouping) => new {a, grouping})
                .SelectMany(t => t.grouping.DefaultIfEmpty(), (t, p) => new {t.a, p!.LastSeenUserName})
                .ToArrayAsync(cancel);

            var adminRanks = await ServerDbContext.AdminRanks.Include(a => a.Flags).ToArrayAsync(cancel);

            return (admins.Select(p => (p.a, p.LastSeenUserName)).ToArray(), adminRanks)!;
        }

        public async Task RemoveAdminAsync(NetUserId userId, CancellationToken cancel = default)
        {
            var admin = await ServerDbContext.Admins.SingleAsync(a => a.UserId == userId.UserId, cancel);
            ServerDbContext.Admins.Remove(admin);

            await ServerDbContext.SaveChangesAsync(cancel);
        }

        public async Task AddAdminAsync(Admin admin, CancellationToken cancel = default)
        {
            ServerDbContext.Admins.Add(admin);

            await ServerDbContext.SaveChangesAsync(cancel);
        }

        public async Task UpdateAdminAsync(Admin admin, CancellationToken cancel = default)
        {
            var existing = await ServerDbContext.Admins.Include(a => a.Flags).SingleAsync(a => a.UserId == admin.UserId, cancel);
            existing.Flags = admin.Flags;
            existing.Title = admin.Title;
            existing.AdminRankId = admin.AdminRankId;

            await ServerDbContext.SaveChangesAsync(cancel);
        }

        public async Task RemoveAdminRankAsync(int rankId, CancellationToken cancel = default)
        {
            var admin = await ServerDbContext.AdminRanks.SingleAsync(a => a.Id == rankId, cancel);
            ServerDbContext.AdminRanks.Remove(admin);

            await ServerDbContext.SaveChangesAsync(cancel);
        }

        public async Task AddAdminRankAsync(AdminRank rank, CancellationToken cancel = default)
        {
            ServerDbContext.AdminRanks.Add(rank);

            await ServerDbContext.SaveChangesAsync(cancel);
        }

        public async Task UpdateAdminRankAsync(AdminRank rank, CancellationToken cancel = default)
        {
            var existing = await ServerDbContext.AdminRanks
                .Include(r => r.Flags)
                .SingleAsync(a => a.Id == rank.Id, cancel);

            existing.Flags = rank.Flags;
            existing.Name = rank.Name;

            await ServerDbContext.SaveChangesAsync(cancel);
        }

        private DbContextOptions<ServerDbContext> CreatePostgresOptions()
        {
            var host = _cfg.GetCVar(CCVars.DatabasePgHost);
            var port = _cfg.GetCVar(CCVars.DatabasePgPort);
            var db = _cfg.GetCVar(CCVars.DatabasePgDatabase);
            var user = _cfg.GetCVar(CCVars.DatabasePgUsername);
            var pass = _cfg.GetCVar(CCVars.DatabasePgPassword);

            var builder = new DbContextOptionsBuilder<ServerDbContext>();
            var connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = db,
                Username = user,
                Password = pass
            }.ConnectionString;
            builder.UseNpgsql(connectionString);
            SetupLogging(builder);
            return builder.Options;
        }

        private DbContextOptions<ServerDbContext> CreateSqliteOptions()
        {
            var builder = new DbContextOptionsBuilder<ServerDbContext>();

            var configPreferencesDbPath = _cfg.GetCVar(CCVars.DatabaseSqliteDbPath);
            var inMemory = _res.UserData.RootDir == null;

            SqliteConnection connection;
            if (!inMemory)
            {
                var finalPreferencesDbPath = Path.Combine(_res.UserData.RootDir!, configPreferencesDbPath);
                connection = new SqliteConnection($"Data Source={finalPreferencesDbPath}");
            }
            else
            {
                connection = new SqliteConnection("Data Source=:memory:");
                // When using an in-memory DB we have to open it manually
                // so EFCore doesn't open, close and wipe it.
                connection.Open();
            }

            builder.UseSqlite(connection);
            SetupLogging(builder);
            return builder.Options;
        }

        private void SetupLogging(DbContextOptionsBuilder<ServerDbContext> builder)
        {
            builder.UseLoggerFactory(_msLoggerFactory);
        }

        private sealed class LoggingProvider : ILoggerProvider
        {
            private readonly ILogManager _logManager;

            public LoggingProvider(ILogManager logManager)
            {
                _logManager = logManager;
            }

            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new MSLogger(_logManager.GetSawmill("db.ef"));
            }
        }

        private sealed class MSLogger : ILogger
        {
            private readonly ISawmill _sawmill;

            public MSLogger(ISawmill sawmill)
            {
                _sawmill = sawmill;
            }

            public void Log<TState>(MSLogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                var lvl = logLevel switch
                {
                    MSLogLevel.Trace => LogLevel.Debug,
                    MSLogLevel.Debug => LogLevel.Debug,
                    // EFCore feels the need to log individual DB commands as "Information" so I'm slapping debug on it.
                    MSLogLevel.Information => LogLevel.Debug,
                    MSLogLevel.Warning => LogLevel.Warning,
                    MSLogLevel.Error => LogLevel.Error,
                    MSLogLevel.Critical => LogLevel.Fatal,
                    MSLogLevel.None => LogLevel.Debug,
                    _ => LogLevel.Debug
                };

                _sawmill.Log(lvl, formatter(state, exception));
            }

            public bool IsEnabled(MSLogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                // TODO: this
                return null!;
            }
        }
    }

    public interface IServerDbManager
    {
        void Init();

        // Preferences
        Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile);
        Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index);

        Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot);

        // Single method for two operations for transaction.
        Task DeleteSlotAndSetSelectedIndex(NetUserId userId, int deleteSlot, int newSlot);
        Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId);

        // Username assignment (for guest accounts, so they persist GUID)
        Task AssignUserIdAsync(string name, NetUserId userId);
        Task<NetUserId?> GetAssignedUserIdAsync(string name);

        // Ban stuff
        Task<ServerBan?> GetServerBanAsync(IPAddress? address, NetUserId? userId);
        Task AddServerBanAsync(ServerBan serverBan);

        // Player records
        Task UpdatePlayerRecordAsync(NetUserId userId, string userName, IPAddress address);
        Task<Player?> GetPlayerRecordByUserName(string userName, CancellationToken cancel = default);
        Task<Player?> GetPlayerRecordByUserId(NetUserId userId, CancellationToken cancel = default);

        // Connection log
        Task AddConnectionLogAsync(NetUserId userId, string userName, IPAddress address);

        // Admins
        Task<Admin?> GetAdminDataForAsync(NetUserId userId, CancellationToken cancel = default);
        Task<AdminRank?> GetAdminRankAsync(int id, CancellationToken cancel = default);

        Task<((Admin, string? lastUserName)[] admins, AdminRank[])> GetAllAdminAndRanksAsync(
            CancellationToken cancel = default);

        Task RemoveAdminAsync(NetUserId userId, CancellationToken cancel = default);
        Task AddAdminAsync(Admin admin, CancellationToken cancel = default);
        Task UpdateAdminAsync(Admin admin, CancellationToken cancel = default);

        Task RemoveAdminRankAsync(int rankId, CancellationToken cancel = default);
        Task AddAdminRankAsync(AdminRank rank, CancellationToken cancel = default);
        Task UpdateAdminRankAsync(AdminRank rank, CancellationToken cancel = default);
    }
}
