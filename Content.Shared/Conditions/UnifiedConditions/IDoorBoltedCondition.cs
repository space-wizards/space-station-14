using Content.Shared.Doors.Components;


namespace Content.Shared.Conditions.UnifiedConditions;

public interface IDoorBoltedCondition : IConditionByEvent<IDoorBoltedCondition>
{
    bool Value { get; }
}

public sealed partial class DoorBoltedConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<MetaDataComponent> entity, ref ConditionEvaluationEvent<IDoorBoltedCondition> args)
    {
        args.Handled = true;

        if (!TryComp(entity.Owner, out DoorBoltComponent? airlock))
            args.Value = 1;
        else
            args.Value = airlock.BoltsDown == args.Condition.Value ? 1 : 0;
    }

    /* SO I am leaving this here. the original code of the condition, which is probably bugged? but i implemented their code as is.
         public bool Condition(EntityUid uid, IEntityManager entityManager)
         {
             if (!entityManager.TryGetComponent(uid, out DoorBoltComponent? airlock))
                 return true;

             return airlock.BoltsDown == Value;
         }
     */
}
