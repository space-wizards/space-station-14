using Content.Shared.GameTicking.Components;

namespace Content.Shared.GameTicking.Rules;

public abstract partial class GameRuleSystem<T> where T: IComponent
{
    protected EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent> QueryActiveRules()
    {
        return EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent>();
    }

    protected EntityQueryEnumerator<DelayedStartRuleComponent, T, GameRuleComponent> QueryDelayedRules()
    {
        return EntityQueryEnumerator<DelayedStartRuleComponent, T, GameRuleComponent>();
    }

    /// <summary>
    /// Queries all gamerules, regardless of if they're active or not.
    /// </summary>
    protected EntityQueryEnumerator<T, GameRuleComponent> QueryAllRules()
    {
        return EntityQueryEnumerator<T, GameRuleComponent>();
    }

    protected void ForceEndSelf(Entity<GameRuleComponent?> entity)
    {
        GameTicker.EndGameRule(entity);
    }

    [Obsolete]
    protected void ForceEndSelf(EntityUid uid, GameRuleComponent? component = null)
    {
        ForceEndSelf((uid, component));
    }
}
