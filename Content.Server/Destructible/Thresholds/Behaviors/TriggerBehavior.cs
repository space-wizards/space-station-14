using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Trigger.Systems;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class TriggerBehavior : IThresholdBehavior
{
    /// <summary>
    /// The trigger key to use when triggering.
    /// </summary>
    [DataField]
    public string? KeyOut { get; set; } = TriggerSystem.DefaultTriggerKey;

    public void Execute(EntityUid owner, DestructibleBehaviorSystem system, EntityUid? cause = null)
    {
        system.TriggerSystem.Trigger(owner, cause, KeyOut);
    }
}
