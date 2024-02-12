using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Database;
using Content.Shared.Players;
using Robust.Shared.Configuration;
using Robust.Server.Player;
using Robust.Shared.Timing;

namespace Content.Server.Chat.V2;

/// <summary>
/// Provides handling of chat rate limiting.
/// </summary>
public interface IChatRateLimiter
{
    /// <summary>
    /// Is the entity's player rate-limited? If so, handle rate-limiting them, including warning admins.
    /// </summary>
    /// <param name="entityUid">The entity we want to check for rate limitation</param>
    /// <param name="reason">The entity we want to check for rate limitation</param>
    /// <returns>True if the entity is rate-limited.</returns>
    public bool IsRateLimited(EntityUid entityUid, out string reason);
}

public sealed class ChatRateLimitingManager : IChatRateLimiter
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IChatManager _chat = default!;

     public bool IsRateLimited(EntityUid entityUid, out string reason)
     {
         reason = "";

         if (!_playerManager.TryGetSessionByEntity(entityUid, out var session))
             return false;

         var data = session.ContentData();

        if (data == null)
            return false;

        var time = _gameTiming.RealTime;

        if (data.MessageCountExpiresAt < time)
        {
            var periodLength = _configuration.GetCVar(CCVars.ChatRateLimitPeriod);
            data.MessageCountExpiresAt = time + TimeSpan.FromSeconds(periodLength);
            data.MessageCount = data.MessageCount / 2; // Backoff from spamming slowly
            data.RateLimitAnnouncedToPlayer = false;
        }

        var maxCount = _configuration.GetCVar(CCVars.ChatRateLimitCount);
        data.MessageCount += 1;

        if (data.MessageCount <= maxCount)
            return false;

        // Breached rate limits, inform admins if configured.
        if (_configuration.GetCVar(CCVars.ChatRateLimitAnnounceAdmins))
        {
            if (data.CanAnnounceToAdminsNextAt < time)
            {
                _chat.SendAdminAlert(Loc.GetString("chat-manager-rate-limit-admin-announcement", ("player", session.Name)));

                var delay = _configuration.GetCVar(CCVars.ChatRateLimitAnnounceAdminsDelay);
                data.CanAnnounceToAdminsNextAt = time + TimeSpan.FromSeconds(delay);
            }
        }

        if (data.RateLimitAnnouncedToPlayer)
            return true;

        reason = Loc.GetString(Loc.GetString("chat-manager-rate-limited"));

        _adminLogger.Add(LogType.ChatRateLimited, LogImpact.Medium, $"Player {session} breached chat rate limits");

        data.RateLimitAnnouncedToPlayer = true;

        return true;
     }
}
