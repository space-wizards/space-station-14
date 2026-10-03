using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IMobStateCondition : ICondition
{
    /// <summary>
    /// The mobstate necessary to fulfill this condition.
    /// </summary>
    MobState Mobstate { get; }
}

/// <summary>
/// Returns true if this entity's current mob state matches the condition's specified mob state.
/// </summary>
public sealed partial class MobStateEntityConditionSystem : ConditionEvaluatorSystem<IMobStateCondition>
{
    public override float Evaluate(IMobStateCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out MobStateComponent? mobState))
            return 0;
        return mobState.CurrentState == condition.Mobstate ? 1 : 0;
    }
}
