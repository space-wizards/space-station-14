using Content.Shared.Conditions;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <summary>
/// Returns true if this entity's current mob state matches the condition's specified mob state.
/// </summary>
public sealed partial class MobStateEntityConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<MobStateComponent> entity, ref ConditionEvaluationEvent args)
    {
        if (args.Handled || args.Condition is not MobStateCondition condition)
            return;
        args.Handled = true;

        args.Value = entity.Comp.CurrentState == condition.Mobstate ? 1 : 0;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class MobStateCondition : EntityConditionBase<MobStateCondition>
{
    /// <summary>
    /// The mobstate necessary to fulfill this condition.
    /// </summary>
    [DataField]
    public MobState Mobstate = MobState.Alive;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-mob-state-condition", ("state", Mobstate));
}
