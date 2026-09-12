using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.GameTicking.Rules;

public abstract partial class GameRuleSystem<T> : EntitySystem where T : IComponent
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected IRobustRandom RobustRandom = default!;
    [Dependency] protected GameTicker GameTicker = default!;
    [Dependency] protected EntityQuery<T> RuleQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, GameRuleAddedEvent>(OnGameRuleAdded);
        SubscribeLocalEvent<T, GameRuleStartedEvent>(OnGameRuleStarted);
        SubscribeLocalEvent<T, GameRuleEndedEvent>(OnGameRuleEnded);
    }

    private void OnGameRuleAdded(Entity<T> entity, ref GameRuleAddedEvent args)
    {
        Added((entity, entity, args.Rule), ref args);
    }

    private void OnGameRuleStarted(Entity<T> entity, ref GameRuleStartedEvent args)
    {
        Started((entity, entity, args.Rule), ref args);
    }

    private void OnGameRuleEnded(Entity<T> entity, ref GameRuleEndedEvent args)
    {
        Ended((entity, entity), ref args);
    }

    [SubscribeLocalEvent]
    private void OnRoundEndTextAppend(ref RoundEndTextAppendEvent ev)
    {
        // We don't query GameRuleComponent since the rule may have ended!
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            AppendRoundEndText((uid, comp), ref ev);
        }
    }

    /// <summary>
    /// Called when the gamerule is added
    /// </summary>
    [Obsolete("Use Entity<T,GameRuleComponent> version instead")]
    protected virtual void Added(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {

    }

    /// <summary>
    /// Called when the gamerule is added
    /// </summary>
    protected virtual void Added(Entity<T,GameRuleComponent> rule, ref GameRuleAddedEvent args)
    {
        Added(rule.Owner, rule.Comp1, rule.Comp2, args);
    }

    /// <summary>
    /// Called when the gamerule begins
    /// </summary>
    [Obsolete("Use Entity<T,GameRuleComponent> version instead")]
    protected virtual void Started(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {

    }

    /// <summary>
    /// Called when the gamerule is added
    /// </summary>
    protected virtual void Started(Entity<T,GameRuleComponent> rule, ref GameRuleStartedEvent args)
    {
        Started(rule.Owner, rule.Comp1, rule.Comp2, args);
    }

    /// <summary>
    /// Called when the gamerule is added
    /// </summary>
    protected virtual void Ended(Entity<T> rule, ref GameRuleEndedEvent args) { }

    /// <summary>w
    /// Called at the end of a round when text needs to be added for a game rule.
    /// </summary>
    protected virtual void AppendRoundEndText(Entity<T> rule, ref RoundEndTextAppendEvent args)
    {

    }

    /// <summary>
    /// Called on an active gamerule entity in the Update function
    /// </summary>
    protected virtual void ActiveTick(EntityUid uid, T component, GameRuleComponent gameRule, float frameTime)
    {

    }

    // TODO: We probably should move this to its own rule system that way we aren't doing an Enumerator for EVERY SINGLE GameRule.
    // TODO: Or have GameTicker query over all GameRules once and then raise to each one :P
    // TODO: Either way something that isn't this...
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<T, ActiveGameRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp1, out _, out var comp2))
        {
            ActiveTick(uid, comp1, comp2, frameTime);
        }
    }
}
