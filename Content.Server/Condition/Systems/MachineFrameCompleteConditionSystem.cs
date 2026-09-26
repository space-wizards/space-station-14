using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;

namespace Content.Server.Condition.Systems;

public sealed partial class MachineFrameCompleteConditionSystem : EntitySystem
{
    [Dependency] private MachineFrameSystem _machineFrameSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MachineFrameComponent> entity,
        ref ConditionEvaluationEvent<IEntityAnchoredCondition> args)
    {
        args.Handled = true;

        args.Value = _machineFrameSystem.IsComplete(entity.Comp) ? 1 : 0;
    }

    /*
     *
            if (!entityManager.TryGetComponent(uid, out MachineFrameComponent? machineFrame))
           return false;

       return entityManager.EntitySysManager.GetEntitySystem<MachineFrameSystem>().IsComplete(machineFrame);
     *
     */
}
