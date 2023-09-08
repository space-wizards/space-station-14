using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Component attached to all gamerule entities.
/// Used to both track the entity as well as store basic data
/// </summary>
[RegisterComponent]
public sealed partial class GameRuleComponent : Component
{
    /// <summary>
    /// Whether or not the rule is active.
    /// Is enabled after <see cref="GameRuleStartedEvent"/> and disabled after <see cref="GameRuleEndedEvent"/>
    /// </summary>
    [DataField("active")]
    public bool Active;

    /// <summary>
    /// Game time when game rule was activated
    /// </summary>
    [DataField("activatedAt", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan ActivatedAt;

    /// <summary>
    /// Whether or not the gamerule finished.
    /// Used for tracking whether a non-active gamerule has been started before.
    /// </summary>
    [DataField("ended")]
    public bool Ended;
}

/// <summary>
/// Raised when a rule is added but hasn't formally begun yet.
/// Good for announcing station events and other such things.
/// </summary>
[ByRefEvent]
public readonly record struct GameRuleAddedEvent(EntityUid RuleEntity, string RuleId);

/// <summary>
/// Raised when the rule actually begins.
/// Player-facing logic should begin here.
/// </summary>
[ByRefEvent]
public readonly record struct GameRuleStartedEvent(EntityUid RuleEntity, string RuleId);

/// <summary>
/// Raised when the rule ends.
/// Do cleanup and other such things here.
/// </summary>
[ByRefEvent]
public readonly record struct GameRuleEndedEvent(EntityUid RuleEntity, string RuleId);
