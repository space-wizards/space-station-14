using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Destructible.Thresholds.Triggers;

/// <summary>
/// A trigger that will activate when the amount of damage received
/// of the specified class is above the specified threshold.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class DamageGroupTrigger : IThresholdTrigger
{
    /// <summary>
    /// The damage group to check for.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DamageGroupPrototype> DamageGroup = default!;

    /// <summary>
    /// The amount of damage at which this threshold will trigger.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Damage = default!;

    public bool Reached(Entity<DamageableComponent> damageable, SharedDestructibleSystem system)
    {
        return damageable.Comp.DamagePerGroup[DamageGroup] >= Damage;
    }
}
