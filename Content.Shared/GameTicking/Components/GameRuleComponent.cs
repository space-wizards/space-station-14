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
    /// Marks a gamerule as "Silent" meaning that it will run and then proceed to delete itself without logging.
    /// This WILL throw a debug assert if it still exists when it's supposed to start!
    /// </summary>
    [DataField]
    public bool Silent;

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
public readonly record struct GameRuleAddedEvent(Entity<GameRuleComponent> Rule, EntProtoId RuleId);

/// <summary>
/// Raised when the rule actually begins.
/// Player-facing logic should begin here.
/// </summary>
[ByRefEvent]
public readonly record struct GameRuleStartedEvent(Entity<GameRuleComponent> Rule, EntProtoId RuleId);

/// <summary>
/// Raised when the rule ends.
/// Do cleanup and other such things here.
/// </summary>
[ByRefEvent]
public readonly record struct GameRuleEndedEvent(EntityUid Rule);

/// <summary>
/// A simple struct to keep track of the Lifespan of a Gamerule in an organized fashion.
/// </summary>
/// <param name="StartTime"></param>
/// <param name="Uid"></param>
/// <param name="Lifetime"></param>
[Access(typeof(GameTicker))]
public record struct GameRule(TimeSpan StartTime, EntityUid Uid, GameRuleLifeStage Lifetime = GameRuleLifeStage.Added) : IComparable<GameRuleLifeStage>, IComparable<GameRule>
{
    /// <summary>
    /// The time that this rule has started, or was added if it hasn't started yet!
    /// </summary>
    public TimeSpan StartTime { get; private set; } = StartTime;

    /// <summary>
    /// EntityUid for this GameRule, it's cheaper memory wise to store an int over the whole ass ProtoId, which is available through MetaDataComp lookup...
    /// </summary>
    public readonly EntityUid Uid = Uid;

    /// <summary>
    /// The time that this rule has started, or was added if it hasn't started yet!
    /// </summary>
    public GameRuleLifeStage LifeStage { get; private set; } = Lifetime;

    /// <summary>
    /// Marks that this GameRule has been started.
    /// </summary>
    /// <param name="time">Timespan which we started the rule.</param>
    /// <returns>True if the game rule successfully started, false if was already started or ended.</returns>
    public bool StartRule(TimeSpan time)
    {
        if (LifeStage > GameRuleLifeStage.Added)
            return false;

        LifeStage = GameRuleLifeStage.Started;
        StartTime = time;
        return true;
    }

    /// <summary>
    /// Marks that this GameRule has ended.
    /// </summary>
    public void EndRule()
    {
        LifeStage = GameRuleLifeStage.Ended;
    }

    public int CompareTo(GameRule other)
    {
        return CompareTo(other.LifeStage);
    }

    public int CompareTo(GameRuleLifeStage other)
    {
        return LifeStage.CompareTo(other);
    }

    public static implicit operator GameRule((TimeSpan Time, EntityUid Rule) tuple)
    {
        return new GameRule(tuple.Time, tuple.Rule);
    }

    public static implicit operator GameRule((TimeSpan Time, EntityUid Rule, GameRuleLifeStage Stage) tuple)
    {
        return new GameRule(tuple.Time, tuple.Rule, tuple.Stage);
    }

    public readonly void Deconstruct(out TimeSpan time, out EntityUid rule)
    {
        time = StartTime;
        rule = Uid;
    }

    public readonly void Deconstruct(out TimeSpan time, out EntityUid rule, out GameRuleLifeStage stage)
    {
        time = StartTime;
        rule = Uid;
        stage = LifeStage;
    }
}

public enum GameRuleLifeStage : byte
{
    Added = 0,
    Started = 1,
    Ended = 2,
}
