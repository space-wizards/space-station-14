using Content.Shared.Damage;
using Content.Shared.DoAfter;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Repairable;

public abstract partial class SharedRepairableSystem : EntitySystem
{
    [Serializable, NetSerializable]
    protected sealed partial class RepairFinishedEvent : SimpleDoAfterEvent
    {
    }

    [Serializable, NetSerializable]
    protected sealed partial class RepairByReplacementFinishedEvent : SimpleDoAfterEvent
    {
        public RepairMaterialSpecifier UsedRepairSpecifier;

        public RepairByReplacementFinishedEvent (RepairMaterialSpecifier usedRepairSpecifier)
        {
            UsedRepairSpecifier = usedRepairSpecifier;
        }

        public override DoAfterEvent Clone()
        {
            RepairByReplacementFinishedEvent newDoAfter = (RepairByReplacementFinishedEvent) base.Clone();
            newDoAfter.UsedRepairSpecifier = this.UsedRepairSpecifier;
            return newDoAfter;
        }
    }
}

[UsedImplicitly]
[DataDefinition, Serializable, NetSerializable]
public sealed partial class RepairMaterialSpecifier
{
    /// <summary>
    ///     All the damage to change information is stored in this <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     If this data-field is specified, it will change damage by this amount instead of setting all damage to 0.
    ///     in order to heal/repair the damage values have to be negative.
    /// </remarks>
    [DataField]
    public DamageSpecifier? Damage;

    /// <summary>
    /// If the entity is stacked, how much of the stack is used at once.
    /// Ignored if the entity does not have a StackComponent
    /// </summary>
    [DataField]
    public int MaterialCost = 1;

    [DataField]
    public int DoAfterDelay = 1;

    /// <summary>
    /// A multiplier that will be applied to the above if an entity is repairing themselves.
    /// </summary>
    [DataField]
    public float SelfRepairPenalty = 3f;

    /// <summary>
    /// Whether or not this entity is allowed to repair itself.
    /// </summary>
    [DataField]
    public bool AllowSelfRepair = true;
}
