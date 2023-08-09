namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed class TriggerBehavior : IThresholdBehavior
{
    public void Execute(EntityUid owner, DestructibleSystem system)
    {
        system.TriggerSystem.Trigger(owner);
    }
}
