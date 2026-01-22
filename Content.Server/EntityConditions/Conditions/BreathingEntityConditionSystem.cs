using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.EntityConditions.Conditions.Body;

namespace Content.Server.EntityConditions.Conditions;

/// <summary>
/// Returns true if this entity is both able to breathe and is currently breathing.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class IsBreathingEntityConditionSystem : EntityConditionSystem<RespiratorComponent, BreathingCondition>
{
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    protected override void Condition(Entity<RespiratorComponent> entity, ref EntityConditionEvent<BreathingCondition> args)
    {
        args.Result = _respirator.IsBreathing(entity.AsNullable());
    }
}
