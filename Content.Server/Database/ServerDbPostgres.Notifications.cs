using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Shared.CCVar;
using Npgsql;
using Robust.Shared.Configuration;

namespace Content.Server.Database;

/// Listens for ban_notification containing the player id and the banning server id using postgres listen/notify.
/// Players a ban_notification got received for get banned, except when the current server id and the one in the notification payload match.

public sealed partial class ServerDbPostgres
{
    /// <summary>
    /// The list of notify channels to subscribe to.
    /// </summary>
    private readonly List<string> _channels =
    [
        BanManager.BanNotificationChannel
    ];

    private NpgsqlConnection? _notificationConnection;

    private readonly CancellationTokenSource _notificationTokenSource = new();
    private TimeSpan _reconnectWaitTime = TimeSpan.Zero;
    private readonly TimeSpan _reconnectWaitIncrease = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Sets up the database connection and the notification handler
    /// </summary>
    private void InitNotificationListener(IConfigurationManager cfg)
    {
        // There currently doesn't seem to be a sane way to share getting the connection string with `ServerDbManager`

        var host = cfg.GetCVar(CCVars.DatabasePgHost);
        var port = cfg.GetCVar(CCVars.DatabasePgPort);
        var db = cfg.GetCVar(CCVars.DatabasePgDatabase);
        var user = cfg.GetCVar(CCVars.DatabasePgUsername);
        var pass = cfg.GetCVar(CCVars.DatabasePgPassword);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = db,
            Username = user,
            Password = pass
        };

        _notificationConnection = new NpgsqlConnection(builder.ConnectionString);
        _notificationConnection.Notification += OnNotification;

        var cancellationToken = _notificationTokenSource.Token;
        Task.Run(() => NotificationListener(cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Listens to the notification channel with basic error handling and reopens the connection if it got closed
    /// </summary>
    private async Task NotificationListener(CancellationToken cancellationToken)
    {
        if (_notificationConnection == null)
            return;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_notificationConnection.State == ConnectionState.Broken)
                await _notificationConnection.CloseAsync();

            if (_notificationConnection.State == ConnectionState.Closed)
            {
                await Task.Delay(_reconnectWaitTime, cancellationToken);
                await _notificationConnection.OpenAsync(cancellationToken);
                _reconnectWaitTime = TimeSpan.Zero;
            }

            try
            {
                foreach (var channel in _channels)
                {
                    await using var cmd = new NpgsqlCommand($"LISTEN {channel}", _notificationConnection);
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                await _notificationConnection.WaitAsync(cancellationToken);
            }
            catch (NpgsqlException e)
            {
                _reconnectWaitTime += _reconnectWaitIncrease;
                LogDbError($"{e.Message}\n{e.StackTrace}");
            }
        }
    }

    private void OnNotification(object _, NpgsqlNotificationEventArgs notification)
    {
        NotificationReceived(notification.Channel, notification.Payload);
    }

    public override void Shutdown()
    {
        _notificationTokenSource.Cancel();
        if (_notificationConnection == null)
            return;

        _notificationConnection.Notification -= OnNotification;
        _notificationConnection.Dispose();
    }
}
