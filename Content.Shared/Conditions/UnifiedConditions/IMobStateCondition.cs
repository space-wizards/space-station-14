using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IMobStateCondition : ICondition<IMobStateCondition>
{
    /// <summary>
    /// The mobstate necessary to fulfill this condition.
    /// </summary>
    MobState Mobstate { get; }
}

/// <summary>
/// Returns true if this entity's current mob state matches the condition's specified mob state.
/// </summary>
public sealed partial class MobStateEntityConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<MobStateComponent> entity, ref ConditionEvaluationEvent<IMobStateCondition> args)
    {
        args.Handled = true;

        args.Value = entity.Comp.CurrentState == args.Condition.Mobstate ? 1 : 0;
    }
}
