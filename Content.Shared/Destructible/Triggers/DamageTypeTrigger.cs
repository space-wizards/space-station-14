using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Destructible.Thresholds.Triggers;

/// <summary>
/// A trigger that will activate when the amount of damage received
/// of the specified type is above the specified threshold.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class DamageTypeTrigger : IThresholdTrigger
{
    /// <summary>
    /// The damage type to check for.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DamageTypePrototype> DamageType = default!;

    /// <summary>
    /// The amount of damage at which this threshold will trigger.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Damage = default!;

    public bool Reached(Entity<DamageableComponent> damageable, SharedDestructibleSystem system)
    {
        return damageable.Comp.Damage.DamageDict.TryGetValue(DamageType, out var damageReceived) &&
               damageReceived >= Damage;
    }
}
