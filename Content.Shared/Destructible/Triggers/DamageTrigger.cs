using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Destructible.Thresholds.Triggers;

/// <summary>
/// A trigger that will activate when the total amount of damage received
/// is above the specified threshold.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class DamageTrigger : IThresholdTrigger
{
    /// <summary>
    /// The amount of damage at which this threshold will trigger.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Damage = default!;

    public bool Reached(Entity<DamageableComponent> damageable, SharedDestructibleSystem system)
    {
        return damageable.Comp.TotalDamage >= Damage;
    }
}
