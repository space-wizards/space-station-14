using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using LogLevel = Robust.Shared.Log.LogLevel;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Content.Server.Database
{
    public interface IServerDbManager
    {
        void Init();

        void Shutdown();

        #region Preferences
        Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile);
        Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index);

        Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot);

        Task SaveAdminOOCColorAsync(NetUserId userId, Color color);

        // Single method for two operations for transaction.
        Task DeleteSlotAndSetSelectedIndex(NetUserId userId, int deleteSlot, int newSlot);
        Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId);
        #endregion

        #region User Ids
        // Username assignment (for guest accounts, so they persist GUID)
        Task AssignUserIdAsync(string name, NetUserId userId);
        Task<NetUserId?> GetAssignedUserIdAsync(string name);
        #endregion

        #region Bans
        /// <summary>
        ///     Looks up a ban by id.
        ///     This will return a pardoned ban as well.
        /// </summary>
        /// <param name="id">The ban id to look for.</param>
        /// <returns>The ban with the given id or null if none exist.</returns>
        Task<ServerBanDef?> GetServerBanAsync(int id);

        /// <summary>
        ///     Looks up an user's most recent received un-pardoned ban.
        ///     This will NOT return a pardoned ban.
        ///     One of <see cref="address"/> or <see cref="userId"/> need to not be null.
        /// </summary>
        /// <param name="address">The ip address of the user.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="hwId">The hardware ID of the user.</param>
        /// <returns>The user's latest received un-pardoned ban, or null if none exist.</returns>
        Task<ServerBanDef?> GetServerBanAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId);

        /// <summary>
        ///     Looks up an user's ban history.
        ///     One of <see cref="address"/> or <see cref="userId"/> need to not be null.
        /// </summary>
        /// <param name="address">The ip address of the user.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="hwId">The HWId of the user.</param>
        /// <param name="includeUnbanned">If true, bans that have been expired or pardoned are also included.</param>
        /// <returns>The user's ban history.</returns>
        Task<List<ServerBanDef>> GetServerBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            bool includeUnbanned=true);

        Task AddServerBanAsync(ServerBanDef serverBan);
        Task AddServerUnbanAsync(ServerUnbanDef serverBan);

        /// <summary>
        /// Update ban exemption information for a player.
        /// </summary>
        /// <remarks>
        /// Database rows are automatically created and removed when appropriate.
        /// </remarks>
        /// <param name="userId">The user to update</param>
        /// <param name="flags">The new ban exemption flags.</param>
        Task UpdateBanExemption(NetUserId userId, ServerBanExemptFlags flags);

        /// <summary>
        /// Get current ban exemption flags for a user
        /// </summary>
        /// <returns><see cref="ServerBanExemptFlags.None"/> if the user is not exempt from any bans.</returns>
        Task<ServerBanExemptFlags> GetBanExemption(NetUserId userId);

        #endregion

        #region Role Bans
        /// <summary>
        ///     Looks up a role ban by id.
        ///     This will return a pardoned role ban as well.
        /// </summary>
        /// <param name="id">The role ban id to look for.</param>
        /// <returns>The role ban with the given id or null if none exist.</returns>
        Task<ServerRoleBanDef?> GetServerRoleBanAsync(int id);

        /// <summary>
        ///     Looks up an user's role ban history.
        ///     This will return pardoned role bans based on the <see cref="includeUnbanned"/> bool.
        ///     Requires one of <see cref="address"/>, <see cref="userId"/>, or <see cref="hwId"/> to not be null.
        /// </summary>
        /// <param name="address">The IP address of the user.</param>
        /// <param name="userId">The NetUserId of the user.</param>
        /// <param name="hwId">The Hardware Id of the user.</param>
        /// <param name="includeUnbanned">Whether expired and pardoned bans are included.</param>
        /// <returns>The user's role ban history.</returns>
        Task<List<ServerRoleBanDef>> GetServerRoleBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            bool includeUnbanned = true);

        Task AddServerRoleBanAsync(ServerRoleBanDef serverBan);
        Task AddServerRoleUnbanAsync(ServerRoleUnbanDef serverBan);
        #endregion

        #region Playtime

        /// <summary>
        /// Look up a player's role timers.
        /// </summary>
        /// <param name="player">The player to get the role timer information from.</param>
        /// <returns>All role timers belonging to the player.</returns>
        Task<List<PlayTime>> GetPlayTimes(Guid player);

        /// <summary>
        /// Update play time information in bulk.
        /// </summary>
        /// <param name="updates">The list of all updates to apply to the database.</param>
        Task UpdatePlayTimes(IReadOnlyCollection<PlayTimeUpdate> updates);

        #endregion

        #region Player Records
        Task UpdatePlayerRecordAsync(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableArray<byte> hwId);
        Task<PlayerRecord?> GetPlayerRecordByUserName(string userName, CancellationToken cancel = default);
        Task<PlayerRecord?> GetPlayerRecordByUserId(NetUserId userId, CancellationToken cancel = default);
        #endregion

        #region Connection Logs
        /// <returns>ID of newly inserted connection log row.</returns>
        Task<int> AddConnectionLogAsync(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableArray<byte> hwId,
            ConnectionDenyReason? denied);

        Task AddServerBanHitsAsync(int connection, IEnumerable<ServerBanDef> bans);

        #endregion

        #region Admin Ranks
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
        #endregion

        #region Rounds

        Task<int> AddNewRound(Server server, params Guid[] playerIds);
        Task<Round> GetRound(int id);
        Task AddRoundPlayers(int id, params Guid[] playerIds);

        #endregion

        #region Admin Logs

        Task<Server> AddOrGetServer(string serverName);
        Task AddAdminLogs(List<AdminLog> logs);
        IAsyncEnumerable<string> GetAdminLogMessages(LogFilter? filter = null);
        IAsyncEnumerable<SharedAdminLog> GetAdminLogs(LogFilter? filter = null);
        IAsyncEnumerable<JsonDocument> GetAdminLogsJson(LogFilter? filter = null);

        #endregion

        #region Whitelist

        Task<bool> GetWhitelistStatusAsync(NetUserId player);

        Task AddToWhitelistAsync(NetUserId player);

        Task RemoveFromWhitelistAsync(NetUserId player);

        #endregion

        #region Uploaded Resources Logs

        Task AddUploadedResourceLogAsync(NetUserId user, DateTime date, string path, byte[] data);

        Task PurgeUploadedResourceLogAsync(int days);

        #endregion

        #region Rules

        Task<DateTime?> GetLastReadRules(NetUserId player);
        Task SetLastReadRules(NetUserId player, DateTime time);

        #endregion

        #region Admin Notes

        Task<int> AddAdminNote(int? roundId, Guid player, string message, Guid createdBy, DateTime createdAt);
        Task<AdminNote?> GetAdminNote(int id);
        Task<List<AdminNote>> GetAdminNotes(Guid player);
        Task DeleteAdminNote(int id, Guid deletedBy, DateTime deletedAt);
        Task EditAdminNote(int id, string message, Guid editedBy, DateTime editedAt);

        #endregion
    }

    public sealed class ServerDbManager : IServerDbManager
    {
        public static readonly Counter DbReadOpsMetric = Metrics.CreateCounter(
            "db_read_ops",
            "Amount of read operations processed by the database manager.");

        public static readonly Counter DbWriteOpsMetric = Metrics.CreateCounter(
            "db_write_ops",
            "Amount of write operations processed by the database manager.");

        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IResourceManager _res = default!;
        [Dependency] private readonly ILogManager _logMgr = default!;

        private ServerDbBase _db = default!;
        private LoggingProvider _msLogProvider = default!;
        private ILoggerFactory _msLoggerFactory = default!;

        private bool _synchronous;
        // When running in integration tests, we'll use a single in-memory SQLite database connection.
        // This is that connection, close it when we shut down.
        private SqliteConnection? _sqliteInMemoryConnection;

        public void Init()
        {
            _msLogProvider = new LoggingProvider(_logMgr);
            _msLoggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(_msLogProvider);
            });

            _synchronous = _cfg.GetCVar(CCVars.DatabaseSynchronous);

            var engine = _cfg.GetCVar(CCVars.DatabaseEngine).ToLower();
            switch (engine)
            {
                case "sqlite":
                    SetupSqlite(out var contextFunc, out var inMemory);
                    _db = new ServerDbSqlite(contextFunc, inMemory);
                    break;
                case "postgres":
                    var pgOptions = CreatePostgresOptions();
                    _db = new ServerDbPostgres(pgOptions);
                    break;
                default:
                    throw new InvalidDataException($"Unknown database engine {engine}.");
            }
        }

        public void Shutdown()
        {
            _sqliteInMemoryConnection?.Dispose();
        }

        public Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.InitPrefsAsync(userId, defaultProfile));
        }

        public Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.SaveSelectedCharacterIndexAsync(userId, index));
        }

        public Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.SaveCharacterSlotAsync(userId, profile, slot));
        }

        public Task DeleteSlotAndSetSelectedIndex(NetUserId userId, int deleteSlot, int newSlot)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.DeleteSlotAndSetSelectedIndex(userId, deleteSlot, newSlot));
        }

        public Task SaveAdminOOCColorAsync(NetUserId userId, Color color)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.SaveAdminOOCColorAsync(userId, color));
        }

        public Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetPlayerPreferencesAsync(userId));
        }

        public Task AssignUserIdAsync(string name, NetUserId userId)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AssignUserIdAsync(name, userId));
        }

        public Task<NetUserId?> GetAssignedUserIdAsync(string name)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetAssignedUserIdAsync(name));
        }

        public Task<ServerBanDef?> GetServerBanAsync(int id)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetServerBanAsync(id));
        }

        public Task<ServerBanDef?> GetServerBanAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetServerBanAsync(address, userId, hwId));
        }

        public Task<List<ServerBanDef>> GetServerBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            bool includeUnbanned=true)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetServerBansAsync(address, userId, hwId, includeUnbanned));
        }

        public Task AddServerBanAsync(ServerBanDef serverBan)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddServerBanAsync(serverBan));
        }

        public Task AddServerUnbanAsync(ServerUnbanDef serverUnban)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddServerUnbanAsync(serverUnban));
        }

        public Task UpdateBanExemption(NetUserId userId, ServerBanExemptFlags flags)
        {
            DbWriteOpsMetric.Inc();
            return _db.UpdateBanExemption(userId, flags);
        }

        public Task<ServerBanExemptFlags> GetBanExemption(NetUserId userId)
        {
            DbReadOpsMetric.Inc();
            return _db.GetBanExemption(userId);
        }

        #region Role Ban
        public Task<ServerRoleBanDef?> GetServerRoleBanAsync(int id)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetServerRoleBanAsync(id));
        }

        public Task<List<ServerRoleBanDef>> GetServerRoleBansAsync(
            IPAddress? address,
            NetUserId? userId,
            ImmutableArray<byte>? hwId,
            bool includeUnbanned = true)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetServerRoleBansAsync(address, userId, hwId, includeUnbanned));
        }

        public Task AddServerRoleBanAsync(ServerRoleBanDef serverRoleBan)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddServerRoleBanAsync(serverRoleBan));
        }

        public Task AddServerRoleUnbanAsync(ServerRoleUnbanDef serverRoleUnban)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddServerRoleUnbanAsync(serverRoleUnban));
        }
        #endregion

        #region Playtime

        public Task<List<PlayTime>> GetPlayTimes(Guid player)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetPlayTimes(player));
        }

        public Task UpdatePlayTimes(IReadOnlyCollection<PlayTimeUpdate> updates)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.UpdatePlayTimes(updates));
        }

        #endregion

        public Task UpdatePlayerRecordAsync(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableArray<byte> hwId)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.UpdatePlayerRecord(userId, userName, address, hwId));
        }

        public Task<PlayerRecord?> GetPlayerRecordByUserName(string userName, CancellationToken cancel = default)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetPlayerRecordByUserName(userName, cancel));
        }

        public Task<PlayerRecord?> GetPlayerRecordByUserId(NetUserId userId, CancellationToken cancel = default)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetPlayerRecordByUserId(userId, cancel));
        }

        public Task<int> AddConnectionLogAsync(
            NetUserId userId,
            string userName,
            IPAddress address,
            ImmutableArray<byte> hwId,
            ConnectionDenyReason? denied)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddConnectionLogAsync(userId, userName, address, hwId, denied));
        }

        public Task AddServerBanHitsAsync(int connection, IEnumerable<ServerBanDef> bans)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddServerBanHitsAsync(connection, bans));
        }

        public Task<Admin?> GetAdminDataForAsync(NetUserId userId, CancellationToken cancel = default)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetAdminDataForAsync(userId, cancel));
        }

        public Task<AdminRank?> GetAdminRankAsync(int id, CancellationToken cancel = default)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetAdminRankDataForAsync(id, cancel));
        }

        public Task<((Admin, string? lastUserName)[] admins, AdminRank[])> GetAllAdminAndRanksAsync(
            CancellationToken cancel = default)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetAllAdminAndRanksAsync(cancel));
        }

        public Task RemoveAdminAsync(NetUserId userId, CancellationToken cancel = default)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.RemoveAdminAsync(userId, cancel));
        }

        public Task AddAdminAsync(Admin admin, CancellationToken cancel = default)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddAdminAsync(admin, cancel));
        }

        public Task UpdateAdminAsync(Admin admin, CancellationToken cancel = default)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.UpdateAdminAsync(admin, cancel));
        }

        public Task RemoveAdminRankAsync(int rankId, CancellationToken cancel = default)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.RemoveAdminRankAsync(rankId, cancel));
        }

        public Task AddAdminRankAsync(AdminRank rank, CancellationToken cancel = default)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddAdminRankAsync(rank, cancel));
        }

        public Task<int> AddNewRound(Server server, params Guid[] playerIds)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddNewRound(server, playerIds));
        }

        public Task<Round> GetRound(int id)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetRound(id));
        }

        public Task AddRoundPlayers(int id, params Guid[] playerIds)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddRoundPlayers(id, playerIds));
        }

        public Task UpdateAdminRankAsync(AdminRank rank, CancellationToken cancel = default)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.UpdateAdminRankAsync(rank, cancel));
        }

        public async Task<Server> AddOrGetServer(string serverName)
        {
            var (server, existed) = await RunDbCommand(() => _db.AddOrGetServer(serverName));
            if (existed)
                DbReadOpsMetric.Inc();
            else
                DbWriteOpsMetric.Inc();

            return server;
        }

        public Task AddAdminLogs(List<AdminLog> logs)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddAdminLogs(logs));
        }

        public IAsyncEnumerable<string> GetAdminLogMessages(LogFilter? filter = null)
        {
            DbReadOpsMetric.Inc();
            return _db.GetAdminLogMessages(filter);
        }

        public IAsyncEnumerable<SharedAdminLog> GetAdminLogs(LogFilter? filter = null)
        {
            DbReadOpsMetric.Inc();
            return _db.GetAdminLogs(filter);
        }

        public IAsyncEnumerable<JsonDocument> GetAdminLogsJson(LogFilter? filter = null)
        {
            DbReadOpsMetric.Inc();
            return _db.GetAdminLogsJson(filter);
        }

        public Task<bool> GetWhitelistStatusAsync(NetUserId player)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetWhitelistStatusAsync(player));
        }

        public Task AddToWhitelistAsync(NetUserId player)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddToWhitelistAsync(player));
        }

        public Task RemoveFromWhitelistAsync(NetUserId player)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.RemoveFromWhitelistAsync(player));
        }

        public Task AddUploadedResourceLogAsync(NetUserId user, DateTime date, string path, byte[] data)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddUploadedResourceLogAsync(user, date, path, data));
        }

        public Task PurgeUploadedResourceLogAsync(int days)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.PurgeUploadedResourceLogAsync(days));
        }

        public Task<DateTime?> GetLastReadRules(NetUserId player)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetLastReadRules(player));
        }

        public Task SetLastReadRules(NetUserId player, DateTime time)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.SetLastReadRules(player, time));
        }

        public Task<int> AddAdminNote(int? roundId, Guid player, string message, Guid createdBy, DateTime createdAt)
        {
            DbWriteOpsMetric.Inc();
            var note = new AdminNote
            {
                RoundId = roundId,
                CreatedById = createdBy,
                LastEditedById = createdBy,
                PlayerUserId = player,
                Message = message,
                CreatedAt = createdAt,
                LastEditedAt = createdAt
            };

            return RunDbCommand(() => _db.AddAdminNote(note));
        }

        public Task<AdminNote?> GetAdminNote(int id)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetAdminNote(id));
        }

        public Task<List<AdminNote>> GetAdminNotes(Guid player)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetAdminNotes(player));
        }

        public Task DeleteAdminNote(int id, Guid deletedBy, DateTime deletedAt)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.DeleteAdminNote(id, deletedBy, deletedAt));
        }

        public Task EditAdminNote(int id, string message, Guid editedBy, DateTime editedAt)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.EditAdminNote(id, message, editedBy, editedAt));
        }

        // Wrapper functions to run DB commands from the thread pool.
        // This will avoid SynchronizationContext capturing and avoid running CPU work on the main thread.
        // For SQLite, this will also enable read parallelization (within limits).
        //
        // If we're configured to be synchronous (for integration tests) we shouldn't thread pool it,
        // as that would make things very random and undeterministic.
        // That only works on SQLite though, since SQLite is internally synchronous anyways.

        private Task<T> RunDbCommand<T>(Func<Task<T>> command)
        {
            if (_synchronous)
                return command();

            return Task.Run(command);
        }

        private Task RunDbCommand(Func<Task> command)
        {
            if (_synchronous)
                return command();

            return Task.Run(command);
        }

        private DbContextOptions<PostgresServerDbContext> CreatePostgresOptions()
        {
            var host = _cfg.GetCVar(CCVars.DatabasePgHost);
            var port = _cfg.GetCVar(CCVars.DatabasePgPort);
            var db = _cfg.GetCVar(CCVars.DatabasePgDatabase);
            var user = _cfg.GetCVar(CCVars.DatabasePgUsername);
            var pass = _cfg.GetCVar(CCVars.DatabasePgPassword);

            var builder = new DbContextOptionsBuilder<PostgresServerDbContext>();
            var connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = db,
                Username = user,
                Password = pass
            }.ConnectionString;

            Logger.DebugS("db.manager", $"Using Postgres \"{host}:{port}/{db}\"");

            builder.UseNpgsql(connectionString);
            SetupLogging(builder);
            return builder.Options;
        }

        private void SetupSqlite(out Func<DbContextOptions<SqliteServerDbContext>> contextFunc, out bool inMemory)
        {
#if USE_SYSTEM_SQLITE
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
#endif

            // Can't re-use the SqliteConnection across multiple threads, so we have to make it every time.

            Func<SqliteConnection> getConnection;

            var configPreferencesDbPath = _cfg.GetCVar(CCVars.DatabaseSqliteDbPath);
            inMemory = _res.UserData.RootDir == null;

            if (!inMemory)
            {
                var finalPreferencesDbPath = Path.Combine(_res.UserData.RootDir!, configPreferencesDbPath);
                Logger.DebugS("db.manager", $"Using SQLite DB \"{finalPreferencesDbPath}\"");
                getConnection = () => new SqliteConnection($"Data Source={finalPreferencesDbPath}");
            }
            else
            {
                Logger.DebugS("db.manager", "Using in-memory SQLite DB");
                _sqliteInMemoryConnection = new SqliteConnection("Data Source=:memory:");
                // When using an in-memory DB we have to open it manually
                // so EFCore doesn't open, close and wipe it every operation.
                _sqliteInMemoryConnection.Open();
                getConnection = () => _sqliteInMemoryConnection;
            }

            contextFunc = () =>
            {
                var builder = new DbContextOptionsBuilder<SqliteServerDbContext>();
                builder.UseSqlite(getConnection());
                SetupLogging(builder);
                return builder.Options;
            };
        }

        private void SetupLogging(DbContextOptionsBuilder builder)
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

            public void Log<TState>(MSLogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
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

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                // TODO: this
                return null;
            }
        }
    }

    public sealed record PlayTimeUpdate(NetUserId User, string Tracker, TimeSpan Time);
}
