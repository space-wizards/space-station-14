using Content.Shared.Timing;
using Content.Shared.Trigger.Components.Conditions;

namespace Content.Shared.Trigger.Systems.Conditions;

public sealed class UseDelayTriggerConditionSystem : TriggerConditionSystem<UseDelayTriggerConditionComponent>
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    protected override void CheckCondition(Entity<UseDelayTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        var cancel = _useDelay.IsDelayed(ent.Owner, ent.Comp.UseDelayId);
        ModifyEvent(ent, cancel, ref args);
    }
}
