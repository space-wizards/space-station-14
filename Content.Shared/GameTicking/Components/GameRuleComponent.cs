using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.GameTicking.Components;

/// <summary>
/// Component attached to all gamerule entities.
/// Used to both track the entity as well as store basic data
/// </summary>
[RegisterComponent, EntityCategory("GameRules")]
public sealed partial class GameRuleComponent : Component
{
    /// <summary>
    /// Game time when game rule was activated
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ActivatedAt;

    /// <summary>
    /// The minimum amount of players needed for this game rule.
    /// </summary>
    [DataField]
    public int MinPlayers;

    /// <summary>
    /// If true, this rule not having enough players will cancel the preset selection.
    /// If false, it will simply not run silently.
    /// </summary>
    [DataField]
    public bool CancelPresetOnTooFewPlayers = true;

    /// <summary>
    /// A delay for when the rule the is started and when the starting logic actually runs.
    /// </summary>
    [DataField]
    public MinMax? Delay;
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
