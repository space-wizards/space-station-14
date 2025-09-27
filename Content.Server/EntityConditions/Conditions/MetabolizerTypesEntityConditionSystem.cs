using System.Linq;
using Content.Server.Body.Components;
using Content.Shared.EntityConditions;
using Content.Shared.EntityConditions.Conditions.Body;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityConditions.Conditions;

public sealed partial class MetabolizerTypeEntityConditionSystem : EntityConditionSystem<MetabolizerComponent, MetabolizerType>
{
    protected override void Condition(Entity<MetabolizerComponent> entity, ref EntityConditionEvent<MetabolizerType> args)
    {
        if (entity.Comp.MetabolizerTypes == null)
            return;

        var intersect = entity.Comp.MetabolizerTypes.Intersect(args.Condition.Type);

        if (!intersect.Any())
            return;

        args.Result = true;
    }
}
