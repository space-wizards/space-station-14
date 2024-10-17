using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.Database;

namespace Content.Server.Administration.Managers;

public sealed partial class UsernameRuleManager
{
    public const string UsernameRuleNotificationChannel = "username_rule_notification";

    private static readonly TimeSpan UsernameRuleNotificationRateLimitTime = TimeSpan.FromSeconds(30);
    private const int UsernameRuleNotificationRateLimitCount = 10;

    private readonly object _usernameRuleNotificationRateLimitStateLock = new();
    private TimeSpan _usernameRuleNotificationRateLimitStart;
    private int _usernameRuleNotificationRateLimitCount;

    private void OnDatabaseNotification(DatabaseNotification notification)
    {
        if (notification.Channel != UsernameRuleNotificationChannel)
        {
            return;
        }

        if (notification.Payload == null)
        {
            _sawmill.Error("got username rule notification with no payload");
            return;
        }

        UsernameRuleNotification data;
        try
        {
            data = JsonSerializer.Deserialize<UsernameRuleNotification>(notification.Payload)
                ?? throw new JsonException("Content is null");
        }
        catch (JsonException e)
        {
            _sawmill.Error($"Got invalid JSON in username rule notification: {e}");
            return;
        }

        if (!CheckRuleRateLimit())
        {
            _sawmill.Verbose("Not processing username rule notification due to rate limit");
            return;
        }

        _taskManager.RunOnMainThread(() => ProcessUsernameRuleNotification(data));
    }

    private async void ProcessUsernameRuleNotification(UsernameRuleNotification data)
    {
        if ((await _entryManager.ServerEntity).Id == data.ServerId)
        {
            _sawmill.Verbose("Not processing username rule notification: came from this server");
            return;
        }

        _sawmill.Verbose($"Processing username rule notification for username rule {data.UsernameRuleId}");
        var usernameRule = await _db.GetServerUsernameRuleAsync(data.UsernameRuleId);
        if (usernameRule == null)
        {
            _sawmill.Warning($"Username rule in notification ({data.UsernameRuleId}) didn't exist?");
            return;
        }

        if (usernameRule.Retired)
        {
            ClearCompiledRegex(usernameRule.Id ?? -1);
            return;
        }

        CacheCompiledRegex(usernameRule.Id ?? -1, usernameRule.Regex, usernameRule.Expression, usernameRule.Message, usernameRule.ExtendToBan);

        KickMatchingConnectedPlayers(data.UsernameRuleId, usernameRule, "username rule notification");
    }

    private bool CheckRuleRateLimit()
    {
        lock (_usernameRuleNotificationRateLimitStateLock)
        {
            var now = _gameTiming.RealTime;
            if (_usernameRuleNotificationRateLimitStart + UsernameRuleNotificationRateLimitTime < now)
            {
                // Rate limit period expired, restart it.
                _usernameRuleNotificationRateLimitCount = 1;
                _usernameRuleNotificationRateLimitStart = now;
                return true;
            }

            _usernameRuleNotificationRateLimitCount += 1;
            return _usernameRuleNotificationRateLimitCount <= UsernameRuleNotificationRateLimitCount;
        }
    }

    private sealed class UsernameRuleNotification
    {
        /// <summary>
        /// The ID of the new username rule object in the database to check.
        /// </summary>
        [JsonRequired, JsonPropertyName("username_rule_id")]
        public int UsernameRuleId { get; init; }

        /// <summary>
        /// The id of the server the username rule was made on.
        /// This is used to avoid double work checking the username rule on the originating server.
        /// </summary>
        /// <remarks>
        /// This is optional in case the username rule was made outside a server (SS14.Admin)
        /// </remarks>
        [JsonPropertyName("server_id")]
        public int? ServerId { get; init; }
    }
}
