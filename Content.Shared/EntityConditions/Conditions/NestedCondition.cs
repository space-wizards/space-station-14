using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

/// <summary>
/// Uses the conditions of an <see cref="EntityConditionPrototype"/>.
/// </summary>
public sealed partial class NestedCondition : EntityCondition
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
public sealed partial class NestedConditionSystem : EntityConditionSystem<TransformComponent, NestedCondition>
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    protected override void Condition(Entity<TransformComponent> ent,
        NestedCondition condition,
        EntityUid? sourceEnt,
        ref bool result)
    {
        result = _conditions.TryCondition(ent, condition.Proto);
    }
}
