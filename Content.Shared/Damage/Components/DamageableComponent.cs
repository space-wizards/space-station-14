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

        [DataField("radiationDamageTypes")]
        public List<ProtoId<DamageTypePrototype>> RadiationDamageTypeIDs = new() { "Radiation" };

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
        public readonly string? ModifierSetId;
        public readonly FixedPoint2? HealthBarThreshold;

        public DamageableComponentState(
            Dictionary<string, FixedPoint2> damageDict,
            string? modifierSetId,
            FixedPoint2? healthBarThreshold)
        {
            DamageDict = damageDict;
            ModifierSetId = modifierSetId;
            HealthBarThreshold = healthBarThreshold;
        }
    }
}
