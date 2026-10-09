using Content.Shared.Doors.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IDoorStateCondition : ICondition
{
    DoorState TargetState { get; }
}

public sealed partial class DoorStateConditionSystem : ConditionEvaluatorSystem<IDoorStateCondition>
{

    public override float Evaluate(IDoorStateCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        return TryComp(entityUid,out DoorComponent? component) ? (component.State == condition.TargetState ? 1 : 0) : 0;
    }
}
