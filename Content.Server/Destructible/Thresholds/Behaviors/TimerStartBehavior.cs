using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Trigger.Systems;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class TimerStartBehavior : IThresholdBehavior
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
    {
        _trigger.ActivateTimerTrigger(owner, cause);
    }
}
