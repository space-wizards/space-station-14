using Content.Server.Explosion.EntitySystems;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class TimerStartBehavior : IThresholdBehavior
{
    public void Execute(EntityUid owner,
        IDependencyCollection collection,
        EntityManager entManager,
        EntityUid? cause = null)
    {
        entManager.System<TriggerSystem>().StartTimer(owner, cause);
    }
}
