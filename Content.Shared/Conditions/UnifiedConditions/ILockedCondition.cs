using Content.Shared.Lock;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface ILockedCondition : ICondition
{

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
            return 0;

        return lockComponent.Locked ? 1 : 0;
    }
}
