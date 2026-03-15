using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Trigger.Systems;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class TriggerBehavior : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    /// <summary>
    /// The trigger key to use when triggering.
    /// </summary>
    [DataField]
    public string? KeyOut { get; set; } = TriggerSystem.DefaultTriggerKey;

    public void Execute(EntityUid owner, EntityUid? cause = null)
    {
        _trigger.Trigger(owner, cause, KeyOut);
    }
}
