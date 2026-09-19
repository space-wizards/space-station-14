using Content.Shared.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

/// <summary>
/// Uses the conditions of an <see cref="EntityConditionPrototype"/>.
/// </summary>
public sealed partial class NestedCondition : EntityConditionBase<NestedCondition>
{
    /// <summary>
    /// The condition prototype to use.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EntityConditionPrototype> Proto;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => prototype.Index(Proto).Condition.EntityConditionGuidebookText(prototype);
}

/// <summary>
/// Handles <see cref="NestedCondition"/>.
/// </summary>
public sealed partial class NestedConditionSystem : EntitySystem
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> ent, ref ConditionEvaluationEvent<NestedCondition> args)
    {
        args.Handled = true;
        args.Value = _conditions.TryCondition(ent, args.Condition.Proto)!=!args.Condition.Inverted?1:0;
    }
}
