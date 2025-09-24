using Content.Server.Body.Components;
using Content.Shared.EntityConditions.Conditions;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityConditions.Conditions;

public sealed partial class MetabolizerTypeEntityConditionSystem : EntityConditionSystem<MetabolizerComponent, Shared.EntityConditions.Conditions.Body.MetabolizerType>
{
    protected override void Condition(Entity<MetabolizerComponent> entity, ref EntityConditionEvent<Shared.EntityConditions.Conditions.Body.MetabolizerType> args)
    {
        if (entity.Comp.MetabolizerTypes == null || !entity.Comp.MetabolizerTypes.Contains(args.Condition.Type))
            return;

        args.Result = true;
    }
}
