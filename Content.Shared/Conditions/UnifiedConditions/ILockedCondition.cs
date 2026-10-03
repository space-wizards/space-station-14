using Content.Shared.Lock;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface ILockedCondition : ICondition<ILockedCondition>
{

}

public sealed partial class LockedConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<LockComponent> entity,
        ref ConditionEvaluationEvent<ILockedCondition> args)
    {
        args.Handled = true;
        args.Value = entity.Comp.Locked ? 1 : 0;
    }

    /*
     if (!entityManager.TryGetComponent(uid, out LockComponent? lockcomp))
           return true;

       return lockcomp.Locked == IsLocked;
     */
}
