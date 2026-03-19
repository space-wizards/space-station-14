using System.Collections.Frozen;
using System.Collections.ObjectModel;
using Content.Shared.Database;

namespace Content.Server.Administration.Logs;

// TODO: MAKE CVAR

/// <summary>
/// Controls which <see cref="LogType"/>s are eligible for condensation and the
/// runtime parameters that control grouping behavior.
/// </summary>
/// <remarks>
/// <para>
/// By default every log type is eligible for condensation unless it appears in
/// <see cref="NeverCondenseTypes"/>. This means a player who repeats any action
/// many times in quick succession will automatically have those events grouped
/// into a single summary entry regardless of the log type.
/// </para>
/// <para>
/// Two thresholds govern how aggressively condensation is applied:
/// <list type="bullet">
///   <item><see cref="AggressiveMinGroupSize"/> (4) — known high-frequency noisy types
///         listed in <see cref="AggressiveTypes"/> condense quickly.</item>
///   <item><see cref="GenericMinGroupSize"/> (8) — all other eligible types require
///         more repetitions before condensation fires, so unusual events are not
///         prematurely collapsed.</item>
/// </list>
/// </para>
/// <para>
/// <b>Things that are always preserved:</b>
/// <list type="bullet">
///   <item>Never condense across different players, servers, or rounds.</item>
///   <item>Never condense <see cref="LogImpact.High"/> or <see cref="LogImpact.Extreme"/> events. TBD</item>
///   <item>Never condense types in <see cref="NeverCondenseTypes"/> — each of those events
///         contains unique content.</item>
///   <item>Condensed output keeps semantic type identity via <see cref="BurstTypeMap"/>
///         where a named burst variant exists.</item>
/// </list>
/// </para>
/// </remarks>
public static class LogCondensationPolicy
{
    /// <summary>
    /// Maximum gap between two consecutive events in the same group before the window is closed.
    /// Events separated by more than this are placed in separate groups.
    /// </summary>
    public static readonly TimeSpan MaxEventGap = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Minimum group size for types listed in <see cref="AggressiveTypes"/>.
    /// These are known high-frequency, low-signal types that should condense quickly.
    /// </summary>
    public const int AggressiveMinGroupSize = 4;

    /// <summary>
    /// Minimum group size for all other eligible types not in <see cref="AggressiveTypes"/>.
    /// A higher threshold ensures unusual or important event sequences are not prematurely
    /// collapsed — a player must genuinely repeat the same action many times before it
    /// gets summarised.
    /// </summary>
    public const int GenericMinGroupSize = 8;

    /// <summary>
    /// Maximum number of sample messages preserved in the condensed event's JSON payload.
    /// </summary>
    public const int MaxSampleMessages = 3;

    /// <summary>
    /// Maximum number of distinct entity names listed in the condensed summary message.
    /// </summary>
    public const int MaxEntityNamesInSummary = 5;

    /// <summary>
    /// Log types that must <b>never</b> be condensed because every event contains
    /// unique, irreplaceable content (e.g. chat message text, connection metadata,
    /// antag assignment details). Adding a type here guarantees each event is
    /// persisted individually regardless of repetition frequency.
    /// </summary>
    public static readonly FrozenSet<LogType> NeverCondenseTypes = new HashSet<LogType>
    {
        // Chat
        LogType.Chat,

        // Player stuff
        LogType.Connection,
        LogType.RoundStartJoin,
        LogType.LateJoin,
        LogType.Respawn,

        // Admin actions
        LogType.AdminMessage,
        LogType.AdminCommands,

        // Role and antag assignment
        LogType.AntagSelection,
        LogType.GhostRoleTaken,
        LogType.Mind,

        // Voting
        LogType.Vote,

        // Station and shuttle events
        LogType.EventAnnounced,
        LogType.EventStarted,
        LogType.EventRan,
        LogType.EventStopped,
        LogType.ShuttleCalled,
        LogType.ShuttleRecalled,
        LogType.EmergencyShuttle,
        LogType.ShuttleImpact,

        // Death, not likely to get condesned anyways
        LogType.Gib,

        // Teleportation
        LogType.Teleport,

        // Identity changes.
        LogType.Identity,

        // Ghost warp destinations
        LogType.GhostWarp,
    }.ToFrozenSet();

    /// <summary>
    /// Types that are known to be high-frequency during normal gameplay.
    /// These use <see cref="AggressiveMinGroupSize"/> (4) so they condense quickly.
    /// All other non-blacklisted types use <see cref="GenericMinGroupSize"/> (8).
    /// </summary>
    public static readonly FrozenSet<LogType> AggressiveTypes = new HashSet<LogType>
    {
        // Interaction spam
        LogType.InteractHand,
        LogType.InteractActivate,
        LogType.InteractUsing,

        // Item manipulation
        LogType.Pickup,
        LogType.Drop,
        LogType.Throw,
        LogType.Landed,

        // Storage
        LogType.Storage,

        // Combat mode toggling spam
        LogType.CombatModeToggle,

        // Melee spam
        LogType.MeleeHit,

        // Slip spam
        LogType.Slip,

    }.ToFrozenSet();

    /// <summary>
    /// Optional mapping from a <see cref="LogType"/> to a dedicated "burst" variant
    /// used for the condensed event. Types not in this map retain their original type.
    /// TODO: This might change
    /// </summary>
    public static readonly ReadOnlyDictionary<LogType, LogType> BurstTypeMap = new(
        new Dictionary<LogType, LogType>
        {
            { LogType.CombatModeToggle, LogType.CombatModeToggleBurst },
            { LogType.InteractHand, LogType.InteractionRepeatBurst },
            { LogType.InteractActivate, LogType.InteractionRepeatBurst },
            { LogType.InteractUsing, LogType.InteractionRepeatBurst },
            { LogType.MeleeHit, LogType.MeleeMissBurst },
        });

    /// <summary>
    /// Returns the LogType to use for a condensed event. If a burst variant exists in
    /// <see cref="BurstTypeMap"/>, returns that; otherwise returns the original type.
    /// </summary>
    public static LogType GetCondensedType(LogType originalType)
    {
        return BurstTypeMap.TryGetValue(originalType, out var burst) ? burst : originalType;
    }

    /// <summary>
    /// Returns the minimum group size required before events of this type are condensed.
    /// </summary>
    public static int GetMinGroupSize(LogType type)
    {
        return AggressiveTypes.Contains(type) ? AggressiveMinGroupSize : GenericMinGroupSize;
    }

    /// <summary>
    /// Returns true if the given log event is eligible for condensation.
    /// <para>
    /// Condensation is <b>opt-out</b>: every type is eligible unless it is in
    /// <see cref="NeverCondenseTypes"/> or has <see cref="LogImpact.High"/> or higher impact.
    /// </para>
    /// </summary>
    public static bool IsEligible(LogType type, LogImpact impact)
    {
        if (impact >= LogImpact.High)
            return false;

        return !NeverCondenseTypes.Contains(type);
    }
}
