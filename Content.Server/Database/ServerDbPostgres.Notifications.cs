using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Npgsql;

namespace Content.Server.Database;

/// Listens for ban_notification containing the player id and the banning server id using postgres listen/notify.
/// Players a ban_notification got received for get banned, except when the current server id and the one in the notification payload match.

public sealed partial class ServerDbPostgres
{
    /// <summary>
    /// The list of notify channels to subscribe to.
    /// </summary>
    private static readonly string[] NotificationChannels =
    [
        BanManager.BanNotificationChannel,
    ];

    private static readonly TimeSpan ReconnectWaitIncrease = TimeSpan.FromSeconds(10);

    private readonly CancellationTokenSource _notificationTokenSource = new();

    private NpgsqlConnection? _notificationConnection;
    private TimeSpan _reconnectWaitTime = TimeSpan.Zero;

    /// <summary>
    /// Sets up the database connection and the notification handler
    /// </summary>
    private void InitNotificationListener(string connectionString)
    {
        _notificationConnection = new NpgsqlConnection(connectionString);
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

        _notifyLog.Verbose("Starting notification listener");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_notificationConnection.State == ConnectionState.Broken)
                {
                    _notifyLog.Debug("Notification listener entered broken state, closing...");
                    await _notificationConnection.CloseAsync();
                }

                if (_notificationConnection.State == ConnectionState.Closed)
                {
                    _notifyLog.Debug("Opening notification listener connection...");
                    if (_reconnectWaitTime != TimeSpan.Zero)
                    {
                        _notifyLog.Verbose($"_reconnectWaitTime is {_reconnectWaitTime}");
                        await Task.Delay(_reconnectWaitTime, cancellationToken);
                    }

                    await _notificationConnection.OpenAsync(cancellationToken);
                    _reconnectWaitTime = TimeSpan.Zero;
                    _notifyLog.Verbose($"Notification connection opened...");
                }

                foreach (var channel in NotificationChannels)
                {
                    _notifyLog.Verbose($"Listening on channel {channel}");
                    await using var cmd = new NpgsqlCommand($"LISTEN {channel}", _notificationConnection);
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    _notifyLog.Verbose("Waiting on notifications...");
                    await _notificationConnection.WaitAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Abort loop on cancel.
                _notifyLog.Verbose($"Shutting down notification listener due to cancellation");
                return;
            }
            catch (Exception e)
            {
                _reconnectWaitTime += ReconnectWaitIncrease;
                _notifyLog.Error($"Error in notification listener: {e}");
            }
        }

        _notificationConnection.Dispose();
    }

    private void OnNotification(object _, NpgsqlNotificationEventArgs notification)
    {
        _notifyLog.Verbose($"Received notification on channel {notification.Channel}");
        NotificationReceived(new DatabaseNotification
        {
            Channel = notification.Channel,
            Payload = notification.Payload,
        });
    }

    public override void Shutdown()
    {
        _notificationTokenSource.Cancel();
        if (_notificationConnection == null)
            return;

        _notificationConnection.Notification -= OnNotification;
    }
}
