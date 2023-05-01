using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.GameTicking.Rules;

public abstract class GameRuleSystem<T> : EntitySystem where T : Component
{
    [Dependency] protected readonly GameTicker GameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, GameRuleAddedEvent>(OnGameRuleAdded);
        SubscribeLocalEvent<T, GameRuleStartedEvent>(OnGameRuleStarted);
        SubscribeLocalEvent<T, GameRuleEndedEvent>(OnGameRuleEnded);
    }

    private void OnGameRuleAdded(EntityUid uid, T component, ref GameRuleAddedEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;
        Added(uid, component, ruleData, args);
    }

    private void OnGameRuleStarted(EntityUid uid, T component, ref GameRuleStartedEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;
        Started(uid, component, ruleData, args);
    }

    private void OnGameRuleEnded(EntityUid uid, T component, ref GameRuleEndedEvent args)
    {
        if (!TryComp<GameRuleComponent>(uid, out var ruleData))
            return;
        Ended(uid, component, ruleData, args);
    }

    /// <summary>
    /// Called when the gamerule is added
    /// </summary>
    protected virtual void Added(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {

    }

    /// <summary>
    /// Called when the gamerule begins
    /// </summary>
    protected virtual void Started(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {

    }

    /// <summary>
    /// Called when the gamerule ends
    /// </summary>
    protected virtual void Ended(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {

    }

    /// <summary>
    /// Called on an active gamerule entity in the Update function
    /// </summary>
    protected virtual void ActiveTick(EntityUid uid, T component, GameRuleComponent gameRule, float frameTime)
    {

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<T, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2))
        {
            if (!GameTicker.IsGameRuleActive(uid, comp2))
                continue;

            ActiveTick(uid, comp1, comp2, frameTime);
        }
    }
}
