using Content.Server.Ame.Components;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;

namespace Content.Server.Condition.Systems;

public sealed partial class AmeShieldIntegrityConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<AmeShieldComponent> entity,
        ref ConditionEvaluationEvent<IAmeShieldIntegrityCondition> args)
    {
        args.Handled = true;
        args.Value = (float)entity.Comp.CoreIntegrity / args.Condition.IntegrityThreshold;
    }

    /*
     *
     *if (!entityManager.TryGetComponent<AmeShieldComponent>(uid, out var shield))
           return true;

       if (CheckAbove)
       {
           return shield.CoreIntegrity >= IntegrityThreshold;
       }
       return shield.CoreIntegrity < IntegrityThreshold;
     *
     */
}
