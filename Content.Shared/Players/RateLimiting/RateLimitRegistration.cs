using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Shared.Players.RateLimiting;

/// <summary>
/// Contains all data necessary to register a rate limit with <see cref="SharedPlayerRateLimitManager.Register"/>.
/// </summary>
public sealed class RateLimitRegistration(
    CVarDef<float> cVarLimitPeriodLength,
    CVarDef<int> cVarLimitCount,
    Action<ICommonSession>? playerLimitedAction,
    CVarDef<int>? cVarAdminAnnounceDelay = null,
    Action<ICommonSession>? adminAnnounceAction = null,
    LogType adminLogType = LogType.RateLimited)
{
    /// <summary>
    /// CVar that controls the period over which the rate limit is counted, measured in seconds.
    /// </summary>
    public readonly CVarDef<float> CVarLimitPeriodLength = cVarLimitPeriodLength;

    /// <summary>
    /// CVar that controls how many actions are allowed in a single rate limit period.
    /// </summary>
    public readonly CVarDef<int> CVarLimitCount = cVarLimitCount;

    /// <summary>
    /// An action that gets invoked when this rate limit has been breached by a player.
    /// </summary>
    /// <remarks>
    /// This can be used for informing players or taking administrative action.
    /// </remarks>
    public readonly Action<ICommonSession>? PlayerLimitedAction = playerLimitedAction;

    /// <summary>
    /// CVar that controls the minimum delay between admin notifications, measured in seconds.
    /// This can be omitted to have no admin notification system.
    /// If the cvar is set to 0, there every breach will be reported.
    /// If the cvar is set to a negative number, admin announcements are disabled.
    /// </summary>
    /// <remarks>
    /// If set, <see cref="AdminAnnounceAction"/> must be set too.
    /// </remarks>
    public readonly CVarDef<int>? CVarAdminAnnounceDelay = cVarAdminAnnounceDelay;

    /// <summary>
    /// An action that gets invoked when a rate limit was breached and admins should be notified.
    /// </summary>
    /// <remarks>
    /// If set, <see cref="CVarAdminAnnounceDelay"/> must be set too.
    /// </remarks>
    public readonly Action<ICommonSession>? AdminAnnounceAction = adminAnnounceAction;

    /// <summary>
    /// Log type used to log rate limit violations to the admin logs system.
    /// </summary>
    public readonly LogType AdminLogType = adminLogType;
}

/// <summary>
/// Result of a rate-limited operation.
/// </summary>
/// <seealso cref="SharedPlayerRateLimitManager.CountAction"/>
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
