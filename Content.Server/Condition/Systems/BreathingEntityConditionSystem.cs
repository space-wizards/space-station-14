using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;

namespace Content.Server.Condition.Systems;

/// <summary>
/// Returns true if this entity is both able to breathe and is currently breathing.
/// </summary>
public sealed partial class IsBreathingEntityConditionSystem : ConditionEvaluatorSystem<IIsBreathingCondition>
{
    [Dependency] private RespiratorSystem _respirator = default!;

    public override float Evaluate(IIsBreathingCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        return _respirator.IsBreathing(entityUid) ? 1 : 0;
    }
}
