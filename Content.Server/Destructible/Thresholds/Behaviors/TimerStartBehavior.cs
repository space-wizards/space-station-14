using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class TimerStartBehavior : IThresholdBehavior
{
    public void Execute(EntityUid owner, DestructibleBehaviorSystem system, EntityUid? cause = null)
    {
        system.TriggerSystem.ActivateTimerTrigger(owner, cause);
    }
}
