using Content.Shared.Body.Components;
using Content.Shared.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <summary>
/// Returns true if this entity is using internals. False if they are not or cannot use internals.
/// </summary>
public sealed partial class InternalsOnEntityConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<InternalsComponent> entity, ref ConditionEvaluationEvent<InternalsCondition> args)
    {
        args.Handled = true;

        args.Value = entity.Comp.GasTankEntity != null ? 1 : 0;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class InternalsCondition : EntityConditionBase<InternalsCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-internals", ("usingInternals", !Inverted));
}
