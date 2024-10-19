using System.Text.Json.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Managers;

public sealed partial class UsernameRuleManager
{
    public const string UsernameRuleNotificationChannel = "username_rule_notification";

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

        DebugTools.AssertNotNull(usernameRule.Id);
        CacheCompiledRegex(usernameRule.Id ?? -1, usernameRule.Regex, usernameRule.Expression, usernameRule.Message, usernameRule.ExtendToBan);

        KickMatchingConnectedPlayers(data.UsernameRuleId, usernameRule, "username rule notification");
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
