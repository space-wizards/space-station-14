using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Access(typeof(RespiratorSystem)), AutoGenerateComponentPause]
    public sealed partial class RespiratorComponent : Component
    {
        /// <summary>
        ///     Volume of our breath in liters
        /// </summary>
        [DataField]
        public float BreathVolume = 0.75f; // Offbrand

        /// <summary>
        ///     How much of the gas we inhale is metabolized? Value range is (0, 1]
        /// </summary>
        [DataField]
        public float Ratio = 1.0f;

        /// <summary>
        ///     The next time that this body will inhale or exhale.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
        public TimeSpan NextUpdate;

        /// <summary>
        ///     The interval between updates. Each update is either inhale or exhale,
        ///     so a full cycle takes twice as long.
        /// </summary>
        [DataField]
        public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2.5); // Offbrand

        /// <summary>
        /// Multiplier applied to <see cref="UpdateInterval"/> for adjusting based on metabolic rate multiplier.
        /// </summary>
        [DataField]
        public float UpdateIntervalMultiplier = 1f;

        /// <summary>
        /// Offbrand - Multiplier applied to <see cref="UpdateInterval"/> for adjusting based on body respiratory rate
        /// </summary>
        [DataField]
        public float BreathRateMultiplier = 1f;

        /// <summary>
        /// Offbrand - Multiplier applied to exhalation to determine how efficient the purging of gases from the body is
        /// </summary>
        [DataField]
        public float ExhaleEfficacyModifier = 1f;

        /// <summary>
        /// Offbrand - Multiplier that determines if an entity is hyperventilating (should audibly breathe)
        /// </summary>
        [DataField]
        public float HyperventilationThreshold = 0.6f;

        /// <summary>
        /// Offbrand - Multiplier applied to <see cref="BreathVolume"/> for adjusting based on body respiratory rate
        /// </summary>
        [ViewVariables]
        public float AdjustedBreathVolume => BreathVolume * BreathRateMultiplier * BreathRateMultiplier;

        /// <summary>
        /// Adjusted update interval based only on body factors, no e.g. stasis
        /// </summary>
        [ViewVariables]
        public TimeSpan BodyAdjustedUpdateInterval => UpdateInterval * BreathRateMultiplier; // Offbrand

        /// <summary>
        /// Adjusted update interval based off of the multiplier value.
        /// </summary>
        [ViewVariables]
        public TimeSpan OverallAdjustedUpdateInterval => UpdateInterval * UpdateIntervalMultiplier * BreathRateMultiplier; // Offbrand

        /// <summary>
        ///     Saturation level. Reduced by UpdateInterval each tick.
        ///     Can be thought of as 'how many seconds you have until you start suffocating' in this configuration.
        /// </summary>
        [DataField]
        public float Saturation = 8.0f; // Offbrand

        /// <summary>
        ///     At what level of saturation will you begin to suffocate?
        /// </summary>
        [DataField]
        public float SuffocationThreshold;

        [DataField]
        public float MaxSaturation = 8.0f; // Offbrand

        [DataField]
        public float MinSaturation = -2.0f;

        // TODO HYPEROXIA?

        [DataField(required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField(required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier DamageRecovery = default!;

        [DataField]
        public TimeSpan GaspEmoteCooldown = TimeSpan.FromSeconds(8);

        [ViewVariables]
        public TimeSpan LastGaspEmoteTime;

        /// <summary>
        ///     The emote when gasps
        /// </summary>
        [DataField]
        public ProtoId<EmotePrototype> GaspEmote = "Gasp";

        /// <summary>
        ///     How many cycles in a row has the mob been under-saturated?
        /// </summary>
        [ViewVariables]
        public int SuffocationCycles = 0;

        /// <summary>
        ///     How many cycles in a row does it take for the suffocation alert to pop up?
        /// </summary>
        [ViewVariables]
        public int SuffocationCycleThreshold = 3;

        [ViewVariables]
        public RespiratorStatus Status = RespiratorStatus.Inhaling;
    }
}

public enum RespiratorStatus
{
    Inhaling,
    Exhaling
}
