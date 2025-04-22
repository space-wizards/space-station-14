using System.Text.Json.Serialization;

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager
{
    // Responsible for ban notification handling.
    // Ban notifications are sent through the database to notify the entire server group that a new ban has been added,
    // so that people will get kicked if they are banned on a different server than the one that placed the ban.
    //
    // Ban notifications are currently sent by a trigger in the database, automatically.

    /// <summary>
    /// The notification channel used to broadcast information about new bans.
    /// </summary>
    public const string BanNotificationChannel = "ban_notification";

    // Rate limit to avoid undue load from mass-ban imports.
    // Only process 10 bans per 30 second interval.
    //
    // I had the idea of maybe binning this by postgres transaction ID,
    // to avoid any possibility of dropping a normal ban by coincidence.
    // Didn't bother implementing this though.
    private static readonly TimeSpan BanNotificationRateLimitTime = TimeSpan.FromSeconds(30);
    private const int BanNotificationRateLimitCount = 10;

    private readonly object _banNotificationRateLimitStateLock = new();
    private TimeSpan _banNotificationRateLimitStart;
    private int _banNotificationRateLimitCount;

    private bool OnDatabaseNotificationEarlyFilter()
    {
        if (!CheckBanRateLimit())
        {
            _sawmill.Verbose("Not processing ban notification due to rate limit");
            return false;
        }

        return true;
    }

    private async void ProcessBanNotification(BanNotificationData data)
    {
        if ((await _entryManager.ServerEntity).Id == data.ServerId)
        {
            _sawmill.Verbose("Not processing ban notification: came from this server");
            return;
        }

        _sawmill.Verbose($"Processing ban notification for ban {data.BanId}");
        var ban = await _db.GetServerBanAsync(data.BanId);
        if (ban == null)
        {
            _sawmill.Warning($"Ban in notification ({data.BanId}) didn't exist?");
            return;
        }

        KickMatchingConnectedPlayers(ban, "ban notification");
    }

    private bool CheckBanRateLimit()
    {
        lock (_banNotificationRateLimitStateLock)
        {
            var now = _gameTiming.RealTime;
            if (_banNotificationRateLimitStart + BanNotificationRateLimitTime < now)
            {
                // Rate limit period expired, restart it.
                _banNotificationRateLimitCount = 1;
                _banNotificationRateLimitStart = now;
                return true;
            }

            _banNotificationRateLimitCount += 1;
            return _banNotificationRateLimitCount <= BanNotificationRateLimitCount;
        }
    }

    /// <summary>
    /// Data sent along the notification channel for a single ban notification.
    /// </summary>
    private sealed class BanNotificationData
    {
        /// <summary>
        /// The ID of the new ban object in the database to check.
        /// </summary>
        [JsonRequired, JsonPropertyName("ban_id")]
        public int BanId { get; init; }

        /// <summary>
        /// The id of the server the ban was made on.
        /// This is used to avoid double work checking the ban on the originating server.
        /// </summary>
        /// <remarks>
        /// This is optional in case the ban was made outside a server (SS14.Admin)
        /// </remarks>
        [JsonPropertyName("server_id")]
        public int? ServerId { get; init; }
    }
}
