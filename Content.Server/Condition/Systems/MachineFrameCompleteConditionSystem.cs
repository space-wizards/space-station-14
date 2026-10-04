using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;

namespace Content.Server.Condition.Systems;

public sealed partial class MachineFrameCompleteConditionSystem : ConditionEvaluatorSystem<IMachineFrameCompleteCondition>
{
    [Dependency] private MachineFrameSystem _machineFrameSystem = default!;

    /*
     *
            if (!entityManager.TryGetComponent(uid, out MachineFrameComponent? machineFrame))
           return false;

       return entityManager.EntitySysManager.GetEntitySystem<MachineFrameSystem>().IsComplete(machineFrame);
     *
     */
    public override float Evaluate(IMachineFrameCompleteCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out MachineFrameComponent? machineFrame))
            return 0;

        return _machineFrameSystem.IsComplete(machineFrame)?1:0;
    }
}
