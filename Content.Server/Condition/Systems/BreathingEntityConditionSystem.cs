using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;

namespace Content.Server.Condition.Systems;

/// <summary>
/// Returns true if this entity is both able to breathe and is currently breathing.
/// </summary>
public sealed partial class IsBreathingEntityConditionSystem : EntitySystem
{
    [Dependency] private RespiratorSystem _respirator = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<RespiratorComponent> entity, ref ConditionEvaluationEvent<IIsBreathingCondition> args)
    {
        args.Handled = true;
        args.Value = _respirator.IsBreathing(entity.AsNullable()) ? 1 : 0;
    }
}
