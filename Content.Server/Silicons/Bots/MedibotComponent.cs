using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Silicons.Bots
{
    [RegisterComponent]
    public sealed class MedibotComponent : Component
    {
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

        public const float EmergencyMedDamageThreshold = 100f;

    }
}
