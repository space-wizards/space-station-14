using Content.Shared.Doors.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IDoorBoltedCondition : ICondition
{
    bool Value { get; }
}

public sealed partial class DoorBoltedConditionSystem : ConditionEvaluatorSystem<IDoorBoltedCondition>
{
    /* SO I am leaving this here. the original code of the condition, which is probably bugged? but i implemented their code as is.
         public bool Condition(EntityUid uid, IEntityManager entityManager)
         {
             if (!entityManager.TryGetComponent(uid, out DoorBoltComponent? airlock))
                 return true;

             return airlock.BoltsDown == Value;
         }
     */
    public override float Evaluate(IDoorBoltedCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out DoorBoltComponent? airlock))
            return 1;

        return airlock.BoltsDown == condition.Value ? 1 : 0;
    }
}
