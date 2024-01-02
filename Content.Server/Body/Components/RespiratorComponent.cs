using Content.Server.Body.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Access(typeof(RespiratorSystem))]
    public sealed partial class RespiratorComponent : Component
    {
        /// <summary>
        ///     Saturation level. Reduced by CycleDelay each tick.
        ///     Can be thought of as 'how many seconds you have until you start suffocating' in this configuration.
        /// </summary>
        [DataField("saturation")]
        public float Saturation = 5.0f;

        /// <summary>
        ///     At what level of saturation will you begin to suffocate?
        /// </summary>
        [DataField("suffocationThreshold")]
        public float SuffocationThreshold;

        [DataField("maxSaturation")]
        public float MaxSaturation = 5.0f;

        [DataField("minSaturation")]
        public float MinSaturation = -2.0f;

        // TODO HYPEROXIA?

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("damageRecovery", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier DamageRecovery = default!;

        [DataField("gaspEmoteCooldown")]
        public TimeSpan GaspEmoteCooldown { get; private set; } = TimeSpan.FromSeconds(8);

        /// <summary>
        ///     The emote when gasps
        /// </summary>
        [DataField("gaspEmote", customTypeSerializer:typeof(PrototypeIdSerializer<EmotePrototype>))]
        public string GaspEmote = "Gasp";

        /// <summary>
        /// Hide the chat message from the chat window, only showing the popup.
        /// This does nothing if WithChat is false.
        /// <summary>
        [DataField("hiddenFromChatWindow")]
        public bool HiddenFromChatWindow = false;

        [ViewVariables]
        public TimeSpan LastGaspEmoteTime;

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

        [DataField("cycleDelay")]
        public float CycleDelay = 2.0f;

        public float AccumulatedFrametime;
    }
}

public enum RespiratorStatus
{
    Inhaling,
    Exhaling
}
