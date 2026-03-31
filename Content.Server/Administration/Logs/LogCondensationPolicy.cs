using System.Collections.Frozen;
using Content.Shared.Database;

namespace Content.Server.Administration.Logs;

/// <summary>
/// Controls which <see cref="LogType"/>s are eligible for condensation and the
/// runtime parameters that control grouping behavior.
/// </summary>
/// <remarks>
/// <para>
/// Condensation is <b>opt-in</b>: only types with an explicit entry in
/// <see cref="Rules"/> are eligible.
/// </para>
/// <para>
/// <b>Things that are always preserved individually:</b>
/// <list type="bullet">
///   <item>Types not listed in <see cref="Rules"/>.</item>
///   <item>Events with <see cref="LogImpact.High"/> or <see cref="LogImpact.Extreme"/>.</item>
///   <item>Events involving more than one player.</item>
///   <item>Events with no associated players.</item>
///   <item>Never condenses across different players, servers, or rounds.</item>
/// </list>
/// </para>
/// </remarks>
public static class LogCondensationPolicy
{
    /// <summary>
    /// Maximum gap between two consecutive events in the same group before the window is closed.
    /// Events separated by more than this are placed in separate groups.
    /// This is the default value; Overridden by the Cvar
    /// <c>adminlogs.condensation_max_gap</c> CVar.
    /// </summary>
    public static readonly TimeSpan DefaultMaxEventGap = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Maximum number of sample messages preserved in the condensed event's JSON payload.
    /// </summary>
    public const int MaxSampleMessages = 3;

    /// <summary>
    /// Maximum number of distinct entity names listed in the condensed summary message.
    /// </summary>
    public const int MaxEntityNamesInSummary = 5;

    /// <summary>
    /// Explicit per-type condensation rules. Only types listed here are eligible
    /// for condensation. All other types are not condensed
    /// </summary>
    public static readonly FrozenDictionary<LogType, CondensationRule> Rules = new Dictionary<LogType, CondensationRule>
    {
        // Interaction spam
        [LogType.InteractHand] = new(MinGroupSize: 4, RequireSinglePlayer: true),
        [LogType.InteractActivate] = new(MinGroupSize: 4, RequireSinglePlayer: true),
        [LogType.InteractUsing] = new(MinGroupSize: 4, RequireSinglePlayer: true),
        [LogType.CombatModeToggle] = new(MinGroupSize: 4, RequireSinglePlayer: true),

        // Storage interactions
        [LogType.Storage] = new(MinGroupSize: 4, RequireSinglePlayer: true),

        // Storage
        [LogType.Pickup] = new(MinGroupSize: 6, RequireSinglePlayer: true),
        [LogType.Drop] = new(MinGroupSize: 6, RequireSinglePlayer: true),
        [LogType.Throw] = new(MinGroupSize: 6, RequireSinglePlayer: true),
        [LogType.Landed] = new(MinGroupSize: 6, RequireSinglePlayer: true),

        // Melee spam
        [LogType.MeleeHit] = new(MinGroupSize: 8, RequireSinglePlayer: true),
    }.ToFrozenDictionary();

    /// <summary>
    /// Returns true if the given log event is eligible for condensation.
    /// <para>
    /// An event is eligible only if its type has an explicit rule in <see cref="Rules"/>,
    /// it has exactly one player (when the rule requires it), and its impact is below
    /// <see cref="LogImpact.High"/>.
    /// </para>
    /// </summary>
    public static bool IsEligible(LogType type, LogImpact impact, int playerCount)
    {
        if (impact >= LogImpact.High)
            return false;

        if (!Rules.TryGetValue(type, out var rule))
            return false;

        if (rule.RequireSinglePlayer && playerCount != 1)
            return false;

        return true;
    }

    /// <summary>
    /// Returns the minimum group size required before events of this type are condensed.
    /// </summary>
    public static int? GetMinGroupSize(LogType type)
    {
        return Rules.TryGetValue(type, out var rule) ? rule.MinGroupSize : null;
    }
}

/// <summary>
/// Defines how a specific <see cref="LogType"/> should be condensed.
/// </summary>
/// When true, only events with exactly one associated player are eligible.
/// Multi-player events are kept individual
public readonly record struct CondensationRule(int MinGroupSize, bool RequireSinglePlayer);
