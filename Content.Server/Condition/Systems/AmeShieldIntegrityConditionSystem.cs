using Content.Server.Ame.Components;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;

namespace Content.Server.Condition.Systems;

public sealed partial class AmeShieldIntegrityConditionSystem : ConditionEvaluatorSystem<IAmeShieldIntegrityCondition>
{
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
    public override float Evaluate(IAmeShieldIntegrityCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp<AmeShieldComponent>(entityUid, out var shield))
            return 1; //see above legacy code...
        return (float)shield.CoreIntegrity / condition.IntegrityThreshold;
    }
}
