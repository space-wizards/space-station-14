using System.Data;
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

public sealed class PostgresNotificationManager : IDisposable, IPostInjectInit
{
    private const string BanNotificationChannel = "ban_notification";
    private const string PostgresDbEngine = "postgres";

    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

#if EXCEPTION_TOLERANCE
    [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif

    private ISawmill? _logger;
    private NpgsqlConnection? _connection;
    private CancellationTokenSource _tokenSource = new CancellationTokenSource();

    public void PostInject()
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
    }

    public void Listen()
    {
        if (_connection == null)
            return;

        var cancellationToken = _tokenSource.Token;
        Task.Run(() => NotificationListener(cancellationToken), cancellationToken);
    }

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

        _taskManager.RunOnMainThread(async () => OnBanNotification(notification.Payload));
    }

    private void OnBanNotification(string payload)
    {
        if (!Guid.TryParse(payload, out var playerId))
            return;

        if (!_playerManager.TryGetSessionById(new NetUserId(playerId), out var player))
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
}
