using Content.Server.Wires;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;
using System.Linq;

namespace Content.Server.Condition.Systems;

public sealed partial class WiresCutConditionSystem : ConditionEvaluatorSystem<IWiresCutCondition>
{
    /*
     * original code for comparison.
             public bool Condition(EntityUid uid, IEntityManager entityManager)
       {
           if (!entityManager.TryGetComponent(uid, out WiresComponent? wires))
               return true;

           foreach (var wire in wires.WiresList)
           {
               switch (Value)
               {
                   case true when !wire.IsCut:
                   case false when wire.IsCut:
                       return false;
               }
           }

           return true;
       }
     *
     */

    public override float Evaluate(IWiresCutCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        var flag = condition.Value;
        if (!TryComp(entityUid, out WiresComponent? wires))
            return 1; // see above legacy code...
        return (float)wires.WiresList.Count(e=>e.IsCut==flag)/(float)wires.WiresList.Count;
    }
}
