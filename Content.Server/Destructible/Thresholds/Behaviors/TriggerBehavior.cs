namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class TriggerBehavior : IThresholdBehavior
{
    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        system.TriggerSystem.Trigger(owner, cause);
    }
}
