using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Medical.Components
{
    /// <summary>
    /// Applies a damage change to the target when used in an interaction.
    /// </summary>
    [RegisterComponent]
    public sealed partial class HealingComponent : Component
    {
        [DataField(required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        /// <remarks>
        ///     This should generally be negative,
        ///     since you're, like, trying to heal damage.
        /// </remarks>
        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float BloodlossModifier = 0.0f;

        /// <summary>
        ///     Restore missing blood.
        /// </summary>
        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ModifyBloodLevel = 0.0f;

        /// <remarks>
        ///     Whitelist bloodtypes that this item can restore
        /// </remarks>
        [DataField]
        public HashSet<ProtoId<ReagentPrototype>> BloodReagentWhitelist = new HashSet<ProtoId<ReagentPrototype>>();

        /// <remarks>
        ///     The supported damage types are specified using a <see cref="DamageContainerPrototype"/>s. For a
        ///     HealingComponent this filters what damage container type this component should work on. If null,
        ///     all damage container types are supported.
        /// </remarks>
        [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<DamageContainerPrototype>))]
        public List<string>? DamageContainers;

        /// <summary>
        ///     How long it takes to apply the damage.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float Delay = 3f;

        /// <summary>
        ///     Delay multiplier when healing yourself.
        /// </summary>
        [DataField]
        public float SelfHealPenaltyMultiplier = 3f;

        /// <summary>
        ///     Sound played on healing begin
        /// </summary>
        [DataField]
        public SoundSpecifier? HealingBeginSound = null;

        /// <summary>
        ///     Sound played on healing end
        /// </summary>
        [DataField]
        public SoundSpecifier? HealingEndSound = null;
    }
}
