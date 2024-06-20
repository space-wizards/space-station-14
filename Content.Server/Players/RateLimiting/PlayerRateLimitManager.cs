using System.Runtime.InteropServices;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Players.RateLimiting;

/// <summary>
/// General-purpose system to rate limit actions taken by clients, such as chat messages.
/// </summary>
/// <remarks>
/// <para>
/// Different categories of rate limits must be registered ahead of time by calling <see cref="Register"/>.
/// Once registered, you can simply call <see cref="CountAction"/> to count a rate-limited action for a player.
/// </para>
/// <para>
/// This system is intended for rate limiting player actions over short periods,
/// to ward against spam that can cause technical issues such as admin client load.
/// It should not be used for in-game actions or similar.
/// </para>
/// <para>
/// Rate limits are reset when a client reconnects.
/// This should not be an issue for the reasonably short rate limit periods this system is intended for.
/// </para>
/// </remarks>
/// <seealso cref="RateLimitRegistration"/>
public sealed class PlayerRateLimitManager
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly Dictionary<string, RegistrationData> _registrations = new();
    private readonly Dictionary<ICommonSession, Dictionary<string, RateLimitDatum>> _rateLimitData = new();

    /// <summary>
    /// Count and validate an action performed by a player against rate limits.
    /// </summary>
    /// <param name="player">The player performing the action.</param>
    /// <param name="key">The key string that was previously used to register a rate limit category.</param>
    /// <returns>Whether the action counted should be blocked due to surpassing rate limits or not.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="player"/> is not a connected player
    /// OR <paramref name="key"/> is not a registered rate limit category.
    /// </exception>
    /// <seealso cref="Register"/>
    public RateLimitStatus CountAction(ICommonSession player, string key)
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
        if (registration.AdminAnnounceDelay is { } cvarAnnounceDelay)
        {
            if (datum.NextAdminAnnounce < time)
            {
                registration.Registration.AdminAnnounceAction!(player);
                datum.NextAdminAnnounce = time + cvarAnnounceDelay;
            }
        }

        if (!datum.Announced)
        {
            registration.Registration.PlayerLimitedAction(player);
            _adminLog.Add(
                registration.Registration.AdminLogType,
                LogImpact.Medium,
                $"Player {player} breached '{key}' rate limit ");

            datum.Announced = true;
        }

        return RateLimitStatus.Blocked;
    }

    /// <summary>
    /// Register a new rate limit category.
    /// </summary>
    /// <param name="key">
    /// The key string that will be referred to later with <see cref="CountAction"/>.
    /// Must be unique and should probably just be a constant somewhere.
    /// </param>
    /// <param name="registration">The data specifying the rate limit's parameters.</param>
    /// <exception cref="InvalidOperationException"><paramref name="key"/> has already been registered.</exception>
    /// <exception cref="ArgumentException"><paramref name="registration"/> is invalid.</exception>
    public void Register(string key, RateLimitRegistration registration)
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
                registration.CVarLimitCount,
                i => data.AdminAnnounceDelay = TimeSpan.FromSeconds(i),
                invokeImmediately: true);
        }

        _registrations.Add(key, data);
    }

    /// <summary>
    /// Initialize the manager's functionality at game startup.
    /// </summary>
    public void Initialize()
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

/// <summary>
/// Contains all data necessary to register a rate limit with <see cref="PlayerRateLimitManager.Register"/>.
/// </summary>
public sealed class RateLimitRegistration
{
    /// <summary>
    /// CVar that controls the period over which the rate limit is counted, measured in seconds.
    /// </summary>
    public required CVarDef<int> CVarLimitPeriodLength { get; init; }

    /// <summary>
    /// CVar that controls how many actions are allowed in a single rate limit period.
    /// </summary>
    public required CVarDef<int> CVarLimitCount { get; init; }

    /// <summary>
    /// An action that gets invoked when this rate limit has been breached by a player.
    /// </summary>
    /// <remarks>
    /// This can be used for informing players or taking administrative action.
    /// </remarks>
    public required Action<ICommonSession> PlayerLimitedAction { get; init; }

    /// <summary>
    /// CVar that controls the minimum delay between admin notifications, measured in seconds.
    /// This can be omitted to have no admin notification system.
    /// </summary>
    /// <remarks>
    /// If set, <see cref="AdminAnnounceAction"/> must be set too.
    /// </remarks>
    public CVarDef<int>? CVarAdminAnnounceDelay { get; init; }

    /// <summary>
    /// An action that gets invoked when a rate limit was breached and admins should be notified.
    /// </summary>
    /// <remarks>
    /// If set, <see cref="CVarAdminAnnounceDelay"/> must be set too.
    /// </remarks>
    public Action<ICommonSession>? AdminAnnounceAction { get; init; }

    /// <summary>
    /// Log type used to log rate limit violations to the admin logs system.
    /// </summary>
    public LogType AdminLogType { get; init; } = LogType.RateLimited;
}

/// <summary>
/// Result of a rate-limited operation.
/// </summary>
/// <seealso cref="PlayerRateLimitManager.CountAction"/>
public enum RateLimitStatus : byte
{
    /// <summary>
    /// The action was not blocked by the rate limit.
    /// </summary>
    Allowed,

    /// <summary>
    /// The action was blocked by the rate limit.
    /// </summary>
    Blocked,
}
