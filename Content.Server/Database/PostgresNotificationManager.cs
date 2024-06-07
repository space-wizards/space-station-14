using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Npgsql;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
#if EXCEPTION_TOLERANCE
using Robust.Shared.Exceptions;
#endif

namespace Content.Server.Database;

/// <summary>
/// Listens for ban_notification containing the player id and the banning server id using postgres listen/notify.
/// Players a ban_notification got received for get banned, except when the current server id and the one in the notification payload match.
/// </summary>
public sealed class PostgresNotificationManager : IDisposable
{
    private const string BanNotificationChannel = "ban_notification";
    private const string PostgresDbEngine = "postgres";

    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ServerDbEntryManager _entryManager = default!;

#if EXCEPTION_TOLERANCE
    [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif

    private ISawmill? _logger;
    private NpgsqlConnection? _connection;

    private readonly CancellationTokenSource _tokenSource = new();

    /// <summary>
    /// Sets up the database connection and the notification handler
    /// </summary>
    public void Init()
    {
        if (!_cfg.GetCVar(CCVars.DatabaseEngine).Equals(PostgresDbEngine, StringComparison.CurrentCultureIgnoreCase))
            return;

        _logger = _logManager.GetSawmill("db.listener");

        var host = _cfg.GetCVar(CCVars.DatabasePgHost);
        var port = _cfg.GetCVar(CCVars.DatabasePgPort);
        var db = _cfg.GetCVar(CCVars.DatabasePgDatabase);
        var user = _cfg.GetCVar(CCVars.DatabasePgUsername);
        var pass = _cfg.GetCVar(CCVars.DatabasePgPassword);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = db,
            Username = user,
            Password = pass
        };

        _connection = new NpgsqlConnection(builder.ConnectionString);
        _connection.Notification += OnNotification;

        var cancellationToken = _tokenSource.Token;
        Task.Run(() => NotificationListener(cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Listens to the notification channel with basic error handling and reopens the connection if it got closed
    /// </summary>
    private async Task NotificationListener(CancellationToken cancellationToken)
    {
        if (_connection == null)
            return;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_connection.State == ConnectionState.Broken)
                await _connection.CloseAsync();

            if (_connection.State == ConnectionState.Closed)
            {
                await _connection.OpenAsync(cancellationToken);
            }

#if EXCEPTION_TOLERANCE
            try
            {
#endif
                await using var cmd = new NpgsqlCommand($"LISTEN {BanNotificationChannel}", _connection);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
                await _connection.WaitAsync(cancellationToken);
#if EXCEPTION_TOLERANCE
            }
            catch (NpgsqlException e)
            {
                _runtimeLog.LogException(e, $"{nameof(PostgresNotificationManager)}.${nameof(NotificationListener)}");
            }
#endif
        }
    }

    private void OnNotification(object _, NpgsqlNotificationEventArgs notification)
    {
        _logger?.Info($"Received postgres notification. Channel: {notification.Channel} Payload: {notification.Payload}");

        // Only one channel so just an if
        if (notification.Channel != BanNotificationChannel)
            return;

        var notificationData = JsonSerializer.Deserialize<BanNotificationData>(notification.Payload);
        if (notificationData == null)
            return;

        // ReSharper disable once AsyncVoidLambda
        _taskManager.RunOnMainThread(async () => await OnBanNotification(notificationData));
    }

    private async Task OnBanNotification(BanNotificationData payload)
    {

        if ((await _entryManager.ServerEntity).Id == payload.ServerId)
            return;

        if (!_playerManager.TryGetSessionById(new NetUserId(payload.PlayerId), out var player))
            return;

        var reason = _loc.GetString("ban-kick-reason");
        _netManager.DisconnectChannel(player.Channel, reason);
        _logger?.Info($"Kicked player {player.Name} ({player.UserId}) for {reason} through ban notification");
    }

    public void Dispose()
    {
        _tokenSource.Cancel();
        if (_connection == null)
            return;

        _connection.Notification -= OnNotification;
        _connection.Dispose();
    }

    private sealed class BanNotificationData
    {
        [JsonRequired, JsonPropertyName("player_id")]
        public Guid PlayerId { get; init; }
        /// <summary>
        /// The id of the server the ban was made on
        /// </summary>
        /// <remarks>This is optional in case the ban was made outside a server (SS14.Admin) </remarks>
        [JsonPropertyName("server_id")]
        public int ServerId { get; init; }
    }
}
