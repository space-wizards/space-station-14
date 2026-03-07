using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Destructible.Thresholds.Triggers;

namespace Content.Server.Destructible.Thresholds;

[DataDefinition]
public sealed partial class DamageThreshold
{
    /// <summary>
    /// Whether or not this threshold was triggered in the previous call to
    /// <see cref="Reached"/>.
    /// </summary>
    [ViewVariables] public bool OldTriggered;

    /// <summary>
    /// Whether or not this threshold has already been triggered.
    /// </summary>
    [DataField]
    public bool Triggered;

    /// <summary>
    /// Whether or not this threshold only triggers once.
    /// If false, it will trigger again once the entity is healed
    /// and then damaged to reach this threshold once again.
    /// It will not repeatedly trigger as damage rises beyond that.
    /// </summary>
    [DataField]
    public bool TriggersOnce;

    /// <summary>
    /// The condition that decides if this threshold has been reached.
    /// Gets evaluated each time the entity's damage changes.
    /// </summary>
    [DataField]
    public IThresholdTrigger? Trigger;

    /// <summary>
    /// Behaviors to activate once this threshold is triggered.
    /// TODO: Replace with EntityEffects.
    /// </summary>
    [DataField]
    public List<IThresholdBehavior> Behaviors = new();
}
