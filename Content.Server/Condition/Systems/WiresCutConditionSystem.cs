using Content.Server.Wires;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;
using System.Linq;

namespace Content.Server.Condition.Systems;

public sealed partial class WiresCutConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<MetaDataComponent> entity, ref ConditionEvaluationEvent<IWiresCutCondition> args)
    {
        args.Handled = true;
        var flag = args.Condition.Value;
        if (!TryComp(entity.Owner, out WiresComponent? wires))
            args.Value = 1;
        else
            args.Value = (float)wires.WiresList.Count(e=>e.IsCut==flag)/(float)wires.WiresList.Count;
    }

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

}
