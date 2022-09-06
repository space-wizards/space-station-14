using System.Threading;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Medical.Components
{
    /// <summary>
    /// Applies a damage change to the target when used in an interaction.
    /// </summary>
    [RegisterComponent]
    public sealed class HealingComponent : Component
    {
        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        /// <remarks>
        ///     This should generally be negative,
        ///     since you're, like, trying to heal damage.
        /// </remarks>
        [DataField("bloodlossModifier")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float BloodlossModifier = 0.0f;

        /// <remarks>
        ///     The supported damage types are specified using a <see cref="DamageContainerPrototype"/>s. For a
        ///     HealingComponent this filters what damage container type this component should work on. If null,
        ///     all damage container types are supported.
        /// </remarks>
        [DataField("damageContainer", customTypeSerializer: typeof(PrototypeIdSerializer<DamageContainerPrototype>))]
        public string? DamageContainerID;

        /// <summary>
        /// How long it takes to apply the damage.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("delay")]
        public float Delay = 3f;

        /// <summary>
        /// Delay multiplier when healing yourself.
        /// </summary>
        [DataField("selfHealPenaltyMultiplier")]
        public float SelfHealPenaltyMultiplier = 3f;

        public CancellationTokenSource? CancelToken = null;

        /// <summary>
        ///     Sound played on healing begin
        /// </summary>
        [DataField("healingBeginSound")]
        public SoundSpecifier? HealingBeginSound = null;

        /// <summary>
        ///     Sound played on healing end
        /// </summary>
        [DataField("healingEndSound")]
        public SoundSpecifier? HealingEndSound = null;
    }
}
