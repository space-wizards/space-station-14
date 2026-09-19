using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Conditions;
using Content.Shared.EntityConditions;
using Content.Shared.EntityConditions.Conditions.Body;

namespace Content.Server.EntityConditions.Conditions;

/// <summary>
/// Returns true if this entity is both able to breathe and is currently breathing.
/// </summary>
public sealed partial class IsBreathingEntityConditionSystem : EntitySystem
{
    [Dependency] private RespiratorSystem _respirator = default!;

    private void Condition(Entity<RespiratorComponent> entity, ref ConditionEvaluationEvent<BreathingCondition> args)
    {
        args.Handled = true;
        args.Value = _respirator.IsBreathing(entity.AsNullable()) ? 1 : 0;
    }
}
