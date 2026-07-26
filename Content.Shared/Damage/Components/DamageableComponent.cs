using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DisplacementMap;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage.Components;

/// <summary>
///     Component that allows entities to be damaged by other entities.
/// </summary>
/// <remarks>
///     Needs to be paired with another component such as <see cref="InjurableComponent" /> to retain damage.
///     Other implementors (e.g. a wounds system) may not update fields on this component at all.
///     Incoming damage can be affected by <see cref="DamageModifierSetId" /> if set.
/// </remarks>
[RegisterComponent]
[NetworkedComponent]
[Access(typeof(DamageableSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class DamageableComponent : Component
{
    /// <summary>
    ///     This <see cref="DamageModifierSetPrototype"/> will be applied to any damage that is dealt to this container,
    ///     unless the damage explicitly ignores resistances.
    /// </summary>
    /// <remarks>
    ///     Though DamageModifierSets can be deserialized directly, we only want to use the prototype version here
    ///     to reduce duplication.
    /// </remarks>
    [DataField("damageModifierSet")]
    public ProtoId<DamageModifierSetPrototype>? DamageModifierSetId;

    /// <summary>
    ///     The current amount of stored damage, for legacy API usage.
    /// </summary>
    /// <remarks>
    ///     You cannot always assume that dealing damage will modify this, or that this reflects anything meaningful about the entity.
    /// </remarks>
    [DataField]
    [Access(typeof(DamageableSystem), Other = AccessPermissions.None)]
    public DamageSpecifier Damage = new();

    /// <summary>
    ///     Damage, indexed by <see cref="DamageGroupPrototype"/> ID keys.
    /// </summary>
    /// <remarks>
    ///     Groups which have no members that are supported by this component will not be present in this
    ///     dictionary.
    /// </remarks>
    [ViewVariables]
    [Access(typeof(DamageableSystem), Other = AccessPermissions.None)]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> DamagePerGroup = new();

    /// <summary>
    ///     The sum of all damages in the DamageableComponent.
    /// </summary>
    [ViewVariables]
    [Access(typeof(DamageableSystem), Other = AccessPermissions.None)]
    public FixedPoint2 TotalDamage;

    [DataField("radiationDamageTypes")]
    // ReSharper disable once UseCollectionExpression - Cannot refactor this as it's a potential sandbox violation.
    public List<ProtoId<DamageTypePrototype>> RadiationDamageTypeIDs = new() { "Radiation" };

    /// <summary>
    /// Sets the displacement map used for any of the DamageVisuals sprites for this entity.
    /// TODO: The entirety of DamageVisualsSystem needs to be rewritten.
    /// </summary>
    [DataField]
    public ProtoId<DisplacementDataPrototype>? Displacement;
}

[Serializable, NetSerializable]
public sealed class DamageableComponentState(
    DamageSpecifier damage,
    ProtoId<DamageModifierSetPrototype>? modifierSetId)
    : ComponentState
{
    public readonly DamageSpecifier Damage = damage;
    public readonly ProtoId<DamageModifierSetPrototype>? ModifierSetId = modifierSetId;
}
