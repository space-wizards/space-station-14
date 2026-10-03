using Content.Shared.Lock;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface ILockedCondition : ICondition
{
    bool IsLocked { get; }
}

public sealed partial class LockedConditionSystem : ConditionEvaluatorSystem<ILockedCondition>
{
    /*
     if (!entityManager.TryGetComponent(uid, out LockComponent? lockcomp))
           return true;

       return lockcomp.Locked == IsLocked;
     */
    public override float Evaluate(ILockedCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out LockComponent? lockComponent))
            return 1;

        return lockComponent.Locked == condition.IsLocked ? 1 : 0;
    }
}
