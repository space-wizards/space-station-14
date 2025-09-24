using System.Linq;
using Content.Server.Body.Components;
using Content.Shared.EntityConditions.Conditions.Body;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityConditions.Conditions;

public sealed partial class MetabolizerTypesEntityConditionSystem : EntityConditionSystem<MetabolizerComponent, MetabolizerTypes>
{
    protected override void Condition(Entity<MetabolizerComponent> entity, ref EntityConditionEvent<MetabolizerTypes> args)
    {
        if (entity.Comp.MetabolizerTypes == null)
            return;

        var intersect = entity.Comp.MetabolizerTypes.Intersect(args.Condition.Types);

        if (!intersect.Any())
            return;

        args.Result = true;
    }
}
