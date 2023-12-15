using System.Runtime.InteropServices;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Chat.Managers;

internal sealed partial class ChatManager
{
    private readonly Dictionary<ICommonSession, RateLimitDatum> _rateLimitData = new();

    public bool HandleRateLimit(ICommonSession player)
    {
        ref var datum = ref CollectionsMarshal.GetValueRefOrAddDefault(_rateLimitData, player, out _);
        var time = _gameTiming.RealTime;
        if (datum.CountExpires < time)
        {
            // Period expired, reset it.
            var periodLength = _configurationManager.GetCVar(CCVars.ChatRateLimitPeriod);
            datum.CountExpires = time + TimeSpan.FromSeconds(periodLength);
            datum.Count = 0;
            datum.Announced = false;
        }

        var maxCount = _configurationManager.GetCVar(CCVars.ChatRateLimitCount);
        datum.Count += 1;

        if (datum.Count <= maxCount)
            return true;

        // Breached rate limits, inform admins if configured.
        if (_configurationManager.GetCVar(CCVars.ChatRateLimitAnnounceAdmins))
        {
            if (datum.NextAdminAnnounce < time)
            {
                SendAdminAlert(Loc.GetString("chat-manager-rate-limit-admin-announcement", ("player", player.Name)));
                var delay = _configurationManager.GetCVar(CCVars.ChatRateLimitAnnounceAdminsDelay);
                datum.NextAdminAnnounce = time + TimeSpan.FromSeconds(delay);
            }
        }

        if (!datum.Announced)
        {
            DispatchServerMessage(player, Loc.GetString("chat-manager-rate-limited"), suppressLog: true);
            _adminLogger.Add(LogType.ChatRateLimited, LogImpact.Medium, $"Player {player} breached chat rate limits");

            datum.Announced = true;
        }

        return false;
    }

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
            _rateLimitData.Remove(e.Session);
    }

    private struct RateLimitDatum
    {
        /// <summary>
        /// Time stamp (relative to <see cref="IGameTiming.RealTime"/>) this rate limit period will expire at.
        /// </summary>
        public TimeSpan CountExpires;

        /// <summary>
        /// How many messages have been sent in the current rate limit period.
        /// </summary>
        public int Count;

        /// <summary>
        /// Have we announced to the player that they've been blocked in this rate limit period?
        /// </summary>
        public bool Announced;

        /// <summary>
        /// Time stamp (relative to <see cref="IGameTiming.RealTime"/>) of the
        /// next time we can send an announcement to admins about rate limit breach.
        /// </summary>
        public TimeSpan NextAdminAnnounce;
    }
}
