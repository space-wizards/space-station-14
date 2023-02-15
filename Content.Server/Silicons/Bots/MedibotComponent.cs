using System.Threading;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Silicons.Bots
{
    [RegisterComponent]
    public sealed class MedibotComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        /// <summary>
        /// Used in NPC logic.
        /// </summary>
        public EntityUid? InjectTarget;

        /// <summary>
        /// Med the bot will inject when UNDER the standard med damage threshold.
        /// </summary>
        [DataField("standardMed", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string StandardMed = "Tricordrazine";

        [DataField("standardMedInjectAmount")]
        public float StandardMedInjectAmount = 15f;
        public const float StandardMedDamageThreshold = 50f;

        /// <summary>
        /// Med the bot will inject when OVER the emergency med damage threshold.
        /// </summary>
        [DataField("emergencyMed", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string EmergencyMed = "Inaprovaline";

        [DataField("emergencyMedInjectAmount")]
        public float EmergencyMedInjectAmount = 15f;

        [DataField("injectDelay")]
        public float InjectDelay = 3f;

        [DataField("injectFinishSound")]
        public SoundSpecifier InjectFinishSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    }
}
