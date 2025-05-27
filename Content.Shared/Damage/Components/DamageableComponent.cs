using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage
{
    /// <summary>
    ///     Component that allows entities to take damage.
    /// </summary>
    /// <remarks>
    ///     The supported damage types are specified using a <see cref="DamageContainerPrototype"/>s. DamageContainers
    ///     may also have resistances to certain damage types, defined via a <see cref="DamageModifierSetPrototype"/>.
    /// </remarks>
    [RegisterComponent]
    [NetworkedComponent]
    [Access(typeof(DamageableSystem), Other = AccessPermissions.ReadExecute)]
    public sealed partial class DamageableComponent : Component
    {
        /// <summary>
        ///     This <see cref="DamageContainerPrototype"/> specifies what damage types are supported by this component.
        ///     If null, all damage types will be supported.
        /// </summary>
        [DataField("damageContainer")]
        public ProtoId<DamageContainerPrototype>? DamageContainerID;

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
        ///     All the damage information is stored in this <see cref="DamageSpecifier"/>.
        /// </summary>
        /// <remarks>
        ///     If this data-field is specified, this allows damageable components to be initialized with non-zero damage.
        /// </remarks>
        [DataField(readOnly: true)] //todo remove this readonly when implementing writing to damagespecifier
        public DamageSpecifier Damage = new();

        /// <summary>
        ///     The current damage used for things like state calculations
        /// </summary>
        /// <remarks>
        ///     Only used on certain damage containers (E.G. People and mice, not vending machines)
        /// </remarks>
        [DataField]
        public DamageSpecifier DamageEffective = new();

        /// <summary>
        ///     If whatever entity this component is contained is should experience softcrit
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool SoftCritEligible = false;

        /// <summary>
        ///     The amount of time it takes for <see cref="DamageEffective"/> to catch up with <see cref="Damage"/>
        ///     This value is decreased with respect to how much damage a biological container has taken,
        ///     becoming 0 if it has died
        ///     Ex: If this is 2 seconds, then a person that has instantly reduced to critical damage from no damage
        ///     would take 1 second to crit, because crit is halfway between perfectly healthy and dead.
        /// </summary>
        /// <remarks>
        ///     This is 15 seconds by default because usually you won't going from full health to nearly dead in
        ///     half a second under most circumstances
        /// </remarks>
        [DataField]
        public TimeSpan DamageLerpTimeZeroDamage = TimeSpan.FromSeconds(30);

        /// <summary>
        ///     Damage, indexed by <see cref="DamageGroupPrototype"/> ID keys.
        /// </summary>
        /// <remarks>
        ///     Groups which have no members that are supported by this component will not be present in this
        ///     dictionary.
        /// </remarks>
        [ViewVariables] public Dictionary<string, FixedPoint2> DamagePerGroup = new();

        /// <summary>
        ///     The sum of all damages in the DamageableComponent.
        /// </summary>
        [ViewVariables]
        public FixedPoint2 TotalDamage;

        [ViewVariables]
        public FixedPoint2 TotalDamageEffective;

        [DataField("radiationDamageTypes")]
        public List<ProtoId<DamageTypePrototype>> RadiationDamageTypeIDs = new() { "Radiation" };

        /// <summary>
        ///     Group types that affect the pain overlay.
        /// </summary>
        ///     TODO: Add support for adding damage types specifically rather than whole damage groups
        [DataField]
        public List<ProtoId<DamageGroupPrototype>> PainDamageGroups = new() { "Brute", "Burn" };

        [DataField]
        public Dictionary<MobState, ProtoId<HealthIconPrototype>> HealthIcons = new()
        {
            { MobState.Alive, "HealthIconFine" },
            { MobState.Critical, "HealthIconCritical" },
            { MobState.Dead, "HealthIconDead" },
        };

        [DataField]
        public ProtoId<HealthIconPrototype> RottingIcon = "HealthIconRotting";

        [DataField]
        public FixedPoint2? HealthBarThreshold;
    }

    [Serializable, NetSerializable]
    public sealed class DamageableComponentState : ComponentState
    {
        public readonly Dictionary<string, FixedPoint2> DamageDict;
        public readonly string? DamageContainerId;
        public readonly string? ModifierSetId;
        public readonly FixedPoint2? HealthBarThreshold;

        public DamageableComponentState(
            Dictionary<string, FixedPoint2> damageDict,
            string? damageContainerId,
            string? modifierSetId,
            FixedPoint2? healthBarThreshold)
        {
            DamageDict = damageDict;
            DamageContainerId = damageContainerId;
            ModifierSetId = modifierSetId;
            HealthBarThreshold = healthBarThreshold;
        }
    }
}
