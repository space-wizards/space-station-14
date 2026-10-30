using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions;

/// <summary>
/// Entity condition API counterpart using <see cref="EntityConditionPrototype"/> instead of <see cref="EntityCondition"/>.
/// </summary>
public sealed partial class SharedEntityConditionsSystem
{
    /// <summary>
    /// <c>TryCondition</c> overload that uses a <see cref="EntityConditionPrototype"/> instead of <see cref="EntityCondition"/>.
    /// </summary>
    public bool TryCondition(EntityUid target, [ForbidLiteral] ProtoId<EntityConditionPrototype> id)
    {
        var proto = ProtoMan.Index(id);
        return TryCondition(target, proto.Condition);
    }
}
