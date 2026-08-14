namespace Content.Shared.Antag;


/// <remarks>
///     Regardless of this value, antags are only initialized after the game rule activates.
///     If a game rule does not have a delayed activation, the antag will be initialized at the same time as this enum.
///     Otherwise, it will not be initialized until the game rule becomes active.
/// </remarks>
public enum AntagSelectionTime : byte
{
    /// <summary>
    /// Antag roles are selected at <see cref="RulePlayerSpawningEvent"/>
    /// </summary>
    PrePlayerSpawn,

    /// <summary>
    /// Antag roles are selected at <see cref="RulePlayerJobsAssignedEvent"/>
    /// </summary>
    JobsAssigned,

    /// <summary>
    /// Antag roles are selected at <see cref="GameRuleStartedEvent"/>
    /// or <see cref="RulePlayerJobsAssignedEvent"/> if the game rule was started before spawning.
    /// This is the latest an antag can be selected.
    /// </summary>
    RuleStarted,

    /// <summary>
    /// Antag roles are *never* selected. Instead, this definition only makes ghost roles.
    /// </summary>
    Never,
}
