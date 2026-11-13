using Content.Shared.GameTicking.Components;

namespace Content.Shared.GameTicking.Rules;

// TODO: Someone should probably make this virtual and make it into a "GameRuleSystem" with all GameRules in it.
// TODO: Then move GameTicker.GameRule into here and have GameRuleSystem<T> inherit, since it already uses it as a dependency...
public abstract partial class GameRuleSystem<T> where T: IComponent
{
    protected EntityQueryEnumerator<T, ActiveGameRuleComponent, GameRuleComponent> QueryActiveRules()
    {
        return EntityQueryEnumerator<T, ActiveGameRuleComponent, GameRuleComponent>();
    }

    protected EntityQueryEnumerator<T, DelayedStartRuleComponent, GameRuleComponent> QueryDelayedRules()
    {
        return EntityQueryEnumerator<T, DelayedStartRuleComponent, GameRuleComponent>();
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

    [Obsolete("Use Entity<T> typed version of this method instead!")]
    protected void ForceEndSelf(EntityUid uid, GameRuleComponent? component = null)
    {
        ForceEndSelf((uid, component));
    }
}
