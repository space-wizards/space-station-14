using Content.Server.Ame.EntitySystems;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[Serializable, DataDefinition]
public sealed partial class AmeFuelLeakBehavior : IThresholdBehavior
{
    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        var fuel = system.EntityManager.System<AmeFuelSystem>();
        fuel.StartLeaking(owner);
    }
}
