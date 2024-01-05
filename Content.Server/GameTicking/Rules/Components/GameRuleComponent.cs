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
    /// Game time when game rule was activated
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ActivatedAt;

    /// <summary>
    /// The minimum amount of players needed for this game rule.
    /// </summary>
    [DataField]
    public int MinPlayers;

    public SortedList<TimeSpan, GameRuleTask> ScheduledTasks = new SortedList<TimeSpan, GameRuleTask>();
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

public sealed class GameRuleTask
{
    public Action<EntityUid, IComponent, GameRuleComponent, float> Action { get; private set; }
    public TimeSpan? Interval { get; private set; }
    public bool Oneshot { get; private set; }

    public GameRuleTask(Action<EntityUid, IComponent, GameRuleComponent, float> action, bool oneshot = false, TimeSpan? interval = null)
    {
        Action = action;
        Interval = interval;
        Oneshot = oneshot;
    }
}
