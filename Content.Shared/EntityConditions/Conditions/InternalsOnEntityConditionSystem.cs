using Content.Shared.Body.Components;
using Content.Shared.EntityEffects;

namespace Content.Shared.EntityConditions.Conditions;

/// <summary>
/// Returns true if the entity is using internals. False if they are not or cannot use internals.
/// </summary>
public sealed partial class InternalsOnEntityConditionSystem : EntityConditionSystem<InternalsComponent, InternalsOn>
{
    protected override void Condition(Entity<InternalsComponent> entity, ref EntityConditionEvent<InternalsOn> args)
    {
        args.Result = entity.Comp.GasTankEntity != null;
    }
}

public sealed partial class InternalsOn : EntityConditionBase<InternalsOn>
{
    // Datafields go here.
}
