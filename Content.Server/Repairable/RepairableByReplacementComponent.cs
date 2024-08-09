
using Content.Shared.Damage;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Repairable;

/// <summary>
/// Heals damage to an entity by using up another entity.
/// <see cref="Repairable.RepairableComponent"/> for healing via tool.
/// </summary>
[RegisterComponent]
public sealed partial class RepairableByReplacementComponent : Component
{
    /// <summary>
    /// An entity with this tag can repair this entity
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> RepairType = string.Empty;

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
