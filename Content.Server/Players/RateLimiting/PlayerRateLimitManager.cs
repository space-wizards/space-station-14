using System.Runtime.InteropServices;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Players.RateLimiting;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Players.RateLimiting;

public sealed class PlayerRateLimitManager : SharedPlayerRateLimitManager
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly Dictionary<string, RegistrationData> _registrations = new();
    private readonly Dictionary<ICommonSession, Dictionary<string, RateLimitDatum>> _rateLimitData = new();

    public override RateLimitStatus CountAction(ICommonSession player, string key)
    {
        if (player.Status == SessionStatus.Disconnected)
            throw new ArgumentException("Player is not connected");
        if (!_registrations.TryGetValue(key, out var registration))
            throw new ArgumentException($"Unregistered key: {key}");

        var playerData = _rateLimitData.GetOrNew(player);
        ref var datum = ref CollectionsMarshal.GetValueRefOrAddDefault(playerData, key, out _);
        var time = _gameTiming.RealTime;
        if (datum.CountExpires < time)
        {
            // Period expired, reset it.
            datum.CountExpires = time + registration.LimitPeriod;
            datum.Count = 0;
            datum.Announced = false;
        }

        datum.Count += 1;

        if (datum.Count <= registration.LimitCount)
            return RateLimitStatus.Allowed;

        // Breached rate limits, inform admins if configured.
        // Negative delays can be used to disable admin announcements.
        if (registration.AdminAnnounceDelay is {TotalSeconds: >= 0} cvarAnnounceDelay)
        {
            if (datum.NextAdminAnnounce < time)
            {
                registration.Registration.AdminAnnounceAction!(player);
                datum.NextAdminAnnounce = time + cvarAnnounceDelay;
            }
        }

        if (!datum.Announced)
        {
            registration.Registration.PlayerLimitedAction?.Invoke(player);
            _adminLog.Add(
                registration.Registration.AdminLogType,
                LogImpact.Medium,
                $"Player {player} breached '{key}' rate limit ");

            datum.Announced = true;
        }

        return RateLimitStatus.Blocked;
    }

    public override void Register(string key, RateLimitRegistration registration)
    {
        if (_registrations.ContainsKey(key))
            throw new InvalidOperationException($"Key already registered: {key}");

        var data = new RegistrationData
        {
            Registration = registration,
        };

        if ((registration.AdminAnnounceAction == null) != (registration.CVarAdminAnnounceDelay == null))
        {
            throw new ArgumentException(
                $"Must set either both {nameof(registration.AdminAnnounceAction)} and {nameof(registration.CVarAdminAnnounceDelay)} or neither");
        }

        _cfg.OnValueChanged(
            registration.CVarLimitCount,
            i => data.LimitCount = i,
            invokeImmediately: true);
        _cfg.OnValueChanged(
            registration.CVarLimitPeriodLength,
            i => data.LimitPeriod = TimeSpan.FromSeconds(i),
            invokeImmediately: true);

        if (registration.CVarAdminAnnounceDelay != null)
        {
            _cfg.OnValueChanged(
                registration.CVarAdminAnnounceDelay,
                i => data.AdminAnnounceDelay = TimeSpan.FromSeconds(i),
                invokeImmediately: true);
        }

        _registrations.Add(key, data);
    }

    public override void Initialize()
    {
        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
            _rateLimitData.Remove(e.Session);
    }

    private sealed class RegistrationData
    {
        public required RateLimitRegistration Registration { get; init; }
        public TimeSpan LimitPeriod { get; set; }
        public int LimitCount { get; set; }
        public TimeSpan? AdminAnnounceDelay { get; set; }
    }

    private struct RateLimitDatum
    {
        /// <summary>
        /// Time stamp (relative to <see cref="IGameTiming.RealTime"/>) this rate limit period will expire at.
        /// </summary>
        public TimeSpan CountExpires;

        /// <summary>
        /// How many actions have been done in the current rate limit period.
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
