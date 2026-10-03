using Content.Shared.Doors.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IDoorStateCondition : IConditionByEvent<IDoorStateCondition>
{
    DoorState TargetState { get; }
}

public sealed partial class DoorStateConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<DoorComponent> entity, ref ConditionEvaluationEvent<IDoorStateCondition> args)
    {
        args.Handled = true;
        args.Value = entity.Comp.State == args.Condition.TargetState ? 1 : 0;
    }
}
