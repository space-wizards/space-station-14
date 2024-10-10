using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using LogLevel = Robust.Shared.Log.LogLevel;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Content.Server.Database
{
    public interface IServerDbManager
    {
        void Init();

        void Shutdown();

        #region Preferences
        Task<PlayerPreferences> InitPrefsAsync(
            NetUserId userId,
            ICharacterProfile defaultProfile,
            CancellationToken cancel);

        Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index);

        Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot);

        Task SaveAdminOOCColorAsync(NetUserId userId, Color color);

        // Single method for two operations for transaction.
        Task DeleteSlotAndSetSelectedIndex(NetUserId userId, int deleteSlot, int newSlot);
        Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId, CancellationToken cancel);
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

        public Task EditServerBan(
            int id,
            string reason,
            NoteSeverity severity,
            DateTimeOffset? expiration,
            Guid editedBy,
            DateTimeOffset editedAt);

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
        Task<ServerBanExemptFlags> GetBanExemption(NetUserId userId, CancellationToken cancel = default);

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

        Task<ServerRoleBanDef> AddServerRoleBanAsync(ServerRoleBanDef serverBan);
        Task AddServerRoleUnbanAsync(ServerRoleUnbanDef serverBan);

        public Task EditServerRoleBan(
            int id,
            string reason,
            NoteSeverity severity,
            DateTimeOffset? expiration,
            Guid editedBy,
            DateTimeOffset editedAt);
        #endregion

        #region Playtime

        /// <summary>
        /// Look up a player's role timers.
        /// </summary>
        /// <param name="player">The player to get the role timer information from.</param>
        /// <param name="cancel"></param>
        /// <returns>All role timers belonging to the player.</returns>
        Task<List<PlayTime>> GetPlayTimes(Guid player, CancellationToken cancel = default);

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
            ConnectionDenyReason? denied,
            int serverId);

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
        Task<int> CountAdminLogs(int round);

        #endregion

        #region Whitelist

        Task<bool> GetWhitelistStatusAsync(NetUserId player);

        Task AddToWhitelistAsync(NetUserId player);

        Task RemoveFromWhitelistAsync(NetUserId player);

        #endregion

        #region Blacklist

        Task<bool> GetBlacklistStatusAsync(NetUserId player);

        Task AddToBlacklistAsync(NetUserId player);

        Task RemoveFromBlacklistAsync(NetUserId player);

        #endregion

        #region Uploaded Resources Logs

        Task AddUploadedResourceLogAsync(NetUserId user, DateTimeOffset date, string path, byte[] data);

        Task PurgeUploadedResourceLogAsync(int days);

        #endregion

        #region Rules

        Task<DateTimeOffset?> GetLastReadRules(NetUserId player);
        Task SetLastReadRules(NetUserId player, DateTimeOffset time);

        #endregion

        #region Admin Notes

        Task<int> AddAdminNote(int? roundId, Guid player, TimeSpan playtimeAtNote, string message, NoteSeverity severity, bool secret, Guid createdBy, DateTimeOffset createdAt, DateTimeOffset? expiryTime);
        Task<int> AddAdminWatchlist(int? roundId, Guid player, TimeSpan playtimeAtNote, string message, Guid createdBy, DateTimeOffset createdAt, DateTimeOffset? expiryTime);
        Task<int> AddAdminMessage(int? roundId, Guid player, TimeSpan playtimeAtNote, string message, Guid createdBy, DateTimeOffset createdAt, DateTimeOffset? expiryTime);
        Task<AdminNoteRecord?> GetAdminNote(int id);
        Task<AdminWatchlistRecord?> GetAdminWatchlist(int id);
        Task<AdminMessageRecord?> GetAdminMessage(int id);
        Task<ServerBanNoteRecord?> GetServerBanAsNoteAsync(int id);
        Task<ServerRoleBanNoteRecord?> GetServerRoleBanAsNoteAsync(int id);
        Task<List<IAdminRemarksRecord>> GetAllAdminRemarks(Guid player);
        Task<List<IAdminRemarksRecord>> GetVisibleAdminNotes(Guid player);
        Task<List<AdminWatchlistRecord>> GetActiveWatchlists(Guid player);
        Task<List<AdminMessageRecord>> GetMessages(Guid player);
        Task EditAdminNote(int id, string message, NoteSeverity severity, bool secret, Guid editedBy, DateTimeOffset editedAt, DateTimeOffset? expiryTime);
        Task EditAdminWatchlist(int id, string message, Guid editedBy, DateTimeOffset editedAt, DateTimeOffset? expiryTime);
        Task EditAdminMessage(int id, string message, Guid editedBy, DateTimeOffset editedAt, DateTimeOffset? expiryTime);
        Task DeleteAdminNote(int id, Guid deletedBy, DateTimeOffset deletedAt);
        Task DeleteAdminWatchlist(int id, Guid deletedBy, DateTimeOffset deletedAt);
        Task DeleteAdminMessage(int id, Guid deletedBy, DateTimeOffset deletedAt);
        Task HideServerBanFromNotes(int id, Guid deletedBy, DateTimeOffset deletedAt);
        Task HideServerRoleBanFromNotes(int id, Guid deletedBy, DateTimeOffset deletedAt);

        /// <summary>
        /// Mark an admin message as being seen by the target player.
        /// </summary>
        /// <param name="id">The database ID of the admin message.</param>
        /// <param name="dismissedToo">
        /// If true, the message is "permanently dismissed" and will not be shown to the player again when they join.
        /// </param>
        Task MarkMessageAsSeen(int id, bool dismissedToo);

        #endregion

        #region Job Whitelists

        Task AddJobWhitelist(Guid player, ProtoId<JobPrototype> job);


        Task<List<string>> GetJobWhitelists(Guid player, CancellationToken cancel = default);
        Task<bool> IsJobWhitelisted(Guid player, ProtoId<JobPrototype> job);

        Task<bool> RemoveJobWhitelist(Guid player, ProtoId<JobPrototype> job);

        #endregion

        #region DB Notifications

        void SubscribeToNotifications(Action<DatabaseNotification> handler);

        /// <summary>
        /// Inject a notification as if it was created by the database. This is intended for testing.
        /// </summary>
        /// <param name="notification">The notification to trigger</param>
        void InjectTestNotification(DatabaseNotification notification);

        #endregion
    }

    /// <summary>
    /// Represents a notification sent between servers via the database layer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Database notifications are a simple system to broadcast messages to an entire server group
    /// backed by the same database. For example, this is used to notify all servers of new ban records.
    /// </para>
    /// <para>
    /// They are currently implemented  by the PostgreSQL <c>NOTIFY</c> and <c>LISTEN</c> commands.
    /// </para>
    /// </remarks>
    public struct DatabaseNotification
    {
        /// <summary>
        /// The channel for the notification. This can be used to differentiate notifications for different purposes.
        /// </summary>
        public required string Channel { get; set; }

        /// <summary>
        /// The actual contents of the notification. Optional.
        /// </summary>
        public string? Payload { get; set; }
    }

    public sealed class ServerDbManager : IServerDbManager
    {
        public static readonly Counter DbReadOpsMetric = Metrics.CreateCounter(
            "db_read_ops",
            "Amount of read operations processed by the database manager.");

        public static readonly Counter DbWriteOpsMetric = Metrics.CreateCounter(
            "db_write_ops",
            "Amount of write operations processed by the database manager.");

        public static readonly Gauge DbActiveOps = Metrics.CreateGauge(
            "db_executing_ops",
            "Amount of active database operations. Note that some operations may be waiting for a database connection.");

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

        private readonly List<Action<DatabaseNotification>> _notificationHandlers = [];

        public void Init()
        {
            _msLogProvider = new LoggingProvider(_logMgr);
            _msLoggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(_msLogProvider);
            });

            _synchronous = _cfg.GetCVar(CCVars.DatabaseSynchronous);

            var engine = _cfg.GetCVar(CCVars.DatabaseEngine).ToLower();
            var opsLog = _logMgr.GetSawmill("db.op");
            var notifyLog = _logMgr.GetSawmill("db.notify");
            switch (engine)
            {
                case "sqlite":
                    SetupSqlite(out var contextFunc, out var inMemory);
                    _db = new ServerDbSqlite(contextFunc, inMemory, _cfg, _synchronous, opsLog);
                    break;
                case "postgres":
                    var (pgOptions, conString) = CreatePostgresOptions();
                    _db = new ServerDbPostgres(pgOptions, conString, _cfg, opsLog, notifyLog);
                    break;
                default:
                    throw new InvalidDataException($"Unknown database engine {engine}.");
            }

            _db.OnNotificationReceived += HandleDatabaseNotification;
        }

        public void Shutdown()
        {
            _db.OnNotificationReceived -= HandleDatabaseNotification;

            _sqliteInMemoryConnection?.Dispose();
            _db.Shutdown();
        }

        public Task<PlayerPreferences> InitPrefsAsync(
            NetUserId userId,
            ICharacterProfile defaultProfile,
            CancellationToken cancel)
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

        public Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId, CancellationToken cancel)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetPlayerPreferencesAsync(userId, cancel));
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

        public Task EditServerBan(int id, string reason, NoteSeverity severity, DateTimeOffset? expiration, Guid editedBy, DateTimeOffset editedAt)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.EditServerBan(id, reason, severity, expiration, editedBy, editedAt));
        }

        public Task UpdateBanExemption(NetUserId userId, ServerBanExemptFlags flags)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.UpdateBanExemption(userId, flags));
        }

        public Task<ServerBanExemptFlags> GetBanExemption(NetUserId userId, CancellationToken cancel = default)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetBanExemption(userId, cancel));
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

        public Task<ServerRoleBanDef> AddServerRoleBanAsync(ServerRoleBanDef serverRoleBan)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddServerRoleBanAsync(serverRoleBan));
        }

        public Task AddServerRoleUnbanAsync(ServerRoleUnbanDef serverRoleUnban)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddServerRoleUnbanAsync(serverRoleUnban));
        }

        public Task EditServerRoleBan(int id, string reason, NoteSeverity severity, DateTimeOffset? expiration, Guid editedBy, DateTimeOffset editedAt)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.EditServerRoleBan(id, reason, severity, expiration, editedBy, editedAt));
        }
        #endregion

        #region Playtime

        public Task<List<PlayTime>> GetPlayTimes(Guid player, CancellationToken cancel)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetPlayTimes(player, cancel));
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
            ConnectionDenyReason? denied,
            int serverId)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddConnectionLogAsync(userId, userName, address, hwId, denied, serverId));
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
            return RunDbCommand(() => _db.GetAdminLogMessages(filter));
        }

        public IAsyncEnumerable<SharedAdminLog> GetAdminLogs(LogFilter? filter = null)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetAdminLogs(filter));
        }

        public IAsyncEnumerable<JsonDocument> GetAdminLogsJson(LogFilter? filter = null)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetAdminLogsJson(filter));
        }

        public Task<int> CountAdminLogs(int round)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.CountAdminLogs(round));
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

        public Task<bool> GetBlacklistStatusAsync(NetUserId player)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetBlacklistStatusAsync(player));
        }

        public Task AddToBlacklistAsync(NetUserId player)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddToBlacklistAsync(player));
        }

        public Task RemoveFromBlacklistAsync(NetUserId player)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.RemoveFromBlacklistAsync(player));
        }

        public Task AddUploadedResourceLogAsync(NetUserId user, DateTimeOffset date, string path, byte[] data)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddUploadedResourceLogAsync(user, date, path, data));
        }

        public Task PurgeUploadedResourceLogAsync(int days)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.PurgeUploadedResourceLogAsync(days));
        }

        public Task<DateTimeOffset?> GetLastReadRules(NetUserId player)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetLastReadRules(player));
        }

        public Task SetLastReadRules(NetUserId player, DateTimeOffset time)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.SetLastReadRules(player, time));
        }

        public Task<int> AddAdminNote(int? roundId, Guid player, TimeSpan playtimeAtNote, string message, NoteSeverity severity, bool secret, Guid createdBy, DateTimeOffset createdAt, DateTimeOffset? expiryTime)
        {
            DbWriteOpsMetric.Inc();
            var note = new AdminNote
            {
                RoundId = roundId,
                CreatedById = createdBy,
                LastEditedById = createdBy,
                PlayerUserId = player,
                PlaytimeAtNote = playtimeAtNote,
                Message = message,
                Severity = severity,
                Secret = secret,
                CreatedAt = createdAt.UtcDateTime,
                LastEditedAt = createdAt.UtcDateTime,
                ExpirationTime = expiryTime?.UtcDateTime
            };

            return RunDbCommand(() => _db.AddAdminNote(note));
        }

        public Task<int> AddAdminWatchlist(int? roundId, Guid player, TimeSpan playtimeAtNote, string message, Guid createdBy, DateTimeOffset createdAt, DateTimeOffset? expiryTime)
        {
            DbWriteOpsMetric.Inc();
            var note = new AdminWatchlist
            {
                RoundId = roundId,
                CreatedById = createdBy,
                LastEditedById = createdBy,
                PlayerUserId = player,
                PlaytimeAtNote = playtimeAtNote,
                Message = message,
                CreatedAt = createdAt.UtcDateTime,
                LastEditedAt = createdAt.UtcDateTime,
                ExpirationTime = expiryTime?.UtcDateTime
            };

            return RunDbCommand(() => _db.AddAdminWatchlist(note));
        }

        public Task<int> AddAdminMessage(int? roundId, Guid player, TimeSpan playtimeAtNote, string message, Guid createdBy, DateTimeOffset createdAt, DateTimeOffset? expiryTime)
        {
            DbWriteOpsMetric.Inc();
            var note = new AdminMessage
            {
                RoundId = roundId,
                CreatedById = createdBy,
                LastEditedById = createdBy,
                PlayerUserId = player,
                PlaytimeAtNote = playtimeAtNote,
                Message = message,
                CreatedAt = createdAt.UtcDateTime,
                LastEditedAt = createdAt.UtcDateTime,
                ExpirationTime = expiryTime?.UtcDateTime
            };

            return RunDbCommand(() => _db.AddAdminMessage(note));
        }

        public Task<AdminNoteRecord?> GetAdminNote(int id)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetAdminNote(id));
        }
        public Task<AdminWatchlistRecord?> GetAdminWatchlist(int id)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetAdminWatchlist(id));
        }
        public Task<AdminMessageRecord?> GetAdminMessage(int id)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetAdminMessage(id));
        }

        public Task<ServerBanNoteRecord?> GetServerBanAsNoteAsync(int id)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetServerBanAsNoteAsync(id));
        }

        public Task<ServerRoleBanNoteRecord?> GetServerRoleBanAsNoteAsync(int id)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetServerRoleBanAsNoteAsync(id));
        }

        public Task<List<IAdminRemarksRecord>> GetAllAdminRemarks(Guid player)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetAllAdminRemarks(player));
        }

        public Task<List<IAdminRemarksRecord>> GetVisibleAdminNotes(Guid player)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetVisibleAdminRemarks(player));
        }

        public Task<List<AdminWatchlistRecord>> GetActiveWatchlists(Guid player)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetActiveWatchlists(player));
        }

        public Task<List<AdminMessageRecord>> GetMessages(Guid player)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetMessages(player));
        }
        public Task EditAdminNote(int id, string message, NoteSeverity severity, bool secret, Guid editedBy, DateTimeOffset editedAt, DateTimeOffset? expiryTime)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.EditAdminNote(id, message, severity, secret, editedBy, editedAt, expiryTime));
        }

        public Task EditAdminWatchlist(int id, string message, Guid editedBy, DateTimeOffset editedAt, DateTimeOffset? expiryTime)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.EditAdminWatchlist(id, message, editedBy, editedAt, expiryTime));
        }

        public Task EditAdminMessage(int id, string message, Guid editedBy, DateTimeOffset editedAt, DateTimeOffset? expiryTime)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.EditAdminMessage(id, message, editedBy, editedAt, expiryTime));
        }

        public Task DeleteAdminNote(int id, Guid deletedBy, DateTimeOffset deletedAt)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.DeleteAdminNote(id, deletedBy, deletedAt));
        }

        public Task DeleteAdminWatchlist(int id, Guid deletedBy, DateTimeOffset deletedAt)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.DeleteAdminWatchlist(id, deletedBy, deletedAt));
        }

        public Task DeleteAdminMessage(int id, Guid deletedBy, DateTimeOffset deletedAt)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.DeleteAdminMessage(id, deletedBy, deletedAt));
        }

        public Task HideServerBanFromNotes(int id, Guid deletedBy, DateTimeOffset deletedAt)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.HideServerBanFromNotes(id, deletedBy, deletedAt));
        }

        public Task HideServerRoleBanFromNotes(int id, Guid deletedBy, DateTimeOffset deletedAt)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.HideServerRoleBanFromNotes(id, deletedBy, deletedAt));
        }

        public Task MarkMessageAsSeen(int id, bool dismissedToo)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.MarkMessageAsSeen(id, dismissedToo));
        }

        public Task AddJobWhitelist(Guid player, ProtoId<JobPrototype> job)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.AddJobWhitelist(player, job));
        }

        public Task<List<string>> GetJobWhitelists(Guid player, CancellationToken cancel = default)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetJobWhitelists(player, cancel));
        }

        public Task<bool> IsJobWhitelisted(Guid player, ProtoId<JobPrototype> job)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.IsJobWhitelisted(player, job));
        }

        public Task<bool> RemoveJobWhitelist(Guid player, ProtoId<JobPrototype> job)
        {
            DbWriteOpsMetric.Inc();
            return RunDbCommand(() => _db.RemoveJobWhitelist(player, job));
        }

        public void SubscribeToNotifications(Action<DatabaseNotification> handler)
        {
            lock (_notificationHandlers)
            {
                _notificationHandlers.Add(handler);
            }
        }

        public void InjectTestNotification(DatabaseNotification notification)
        {
            HandleDatabaseNotification(notification);
        }

        private async void HandleDatabaseNotification(DatabaseNotification notification)
        {
            lock (_notificationHandlers)
            {
                foreach (var handler in _notificationHandlers)
                {
                    handler(notification);
                }
            }
        }

        // Wrapper functions to run DB commands from the thread pool.
        // This will avoid SynchronizationContext capturing and avoid running CPU work on the main thread.
        // For SQLite, this will also enable read parallelization (within limits).
        //
        // If we're configured to be synchronous (for integration tests) we shouldn't thread pool it,
        // as that would make things very random and undeterministic.
        // That only works on SQLite though, since SQLite is internally synchronous anyways.

        private async Task<T> RunDbCommand<T>(Func<Task<T>> command)
        {
            using var _ = DbActiveOps.TrackInProgress();

            if (_synchronous)
                return await RunDbCommandCoreSync(command);

            return await Task.Run(command);
        }

        private async Task RunDbCommand(Func<Task> command)
        {
            using var _ = DbActiveOps.TrackInProgress();

            if (_synchronous)
            {
                await RunDbCommandCoreSync(command);
                return;
            }

            await Task.Run(command);
        }

        private static T RunDbCommandCoreSync<T>(Func<T> command) where T : IAsyncResult
        {
            var task = command();
            if (!task.IsCompleted)
            {
                // We can't just do BlockWaitOnTask here, because that could cause deadlocks.
                // This flag is only intended for integration tests. If we trip this, it's a bug.
                throw new InvalidOperationException(
                    "Database task is running asynchronously. " +
                    "This should be impossible when the database is set to synchronous.");
            }

            return task;
        }

        private IAsyncEnumerable<T> RunDbCommand<T>(Func<IAsyncEnumerable<T>> command)
        {
            var enumerable = command();
            if (_synchronous)
                return new SyncAsyncEnumerable<T>(enumerable);

            return enumerable;
        }

        private (DbContextOptions<PostgresServerDbContext> options, string connectionString) CreatePostgresOptions()
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
            return (builder.Options, connectionString);
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

    internal sealed class SyncAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _enumerable;

        public SyncAsyncEnumerable(IAsyncEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new Enumerator(_enumerable.GetAsyncEnumerator(cancellationToken));
        }

        private sealed class Enumerator : IAsyncEnumerator<T>
        {
            private readonly IAsyncEnumerator<T> _enumerator;

            public Enumerator(IAsyncEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
            }

            public ValueTask DisposeAsync()
            {
                var task = _enumerator.DisposeAsync();
                if (!task.IsCompleted)
                    throw new InvalidOperationException("DisposeAsync did not complete synchronously.");

                return task;
            }

            public ValueTask<bool> MoveNextAsync()
            {
                var task = _enumerator.MoveNextAsync();
                if (!task.IsCompleted)
                    throw new InvalidOperationException("MoveNextAsync did not complete synchronously.");

                return task;
            }

            public T Current => _enumerator.Current;
        }
    }
}
