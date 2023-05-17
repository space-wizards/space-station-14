using Content.Shared.Construction.Prototypes;
using Content.Shared.Power;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed class ChargerComponent : Component
    {
        [ViewVariables]
        public CellChargerStatus Status;

        /// <summary>
        /// The charge rate of the charger, in watts
        /// </summary>

        [DataField("chargeRate")]
        public float ChargeRate = 20.0f;

        /// <summary>
        /// The charge rate with no machine upgrades
        /// </summary>

        [DataField("baseChargeRate")]
        public float BaseChargeRate = 20.0f;

        /// <summary>
        /// The machine part that affects the charge rate multiplier of the charger
        /// </summary>
        [DataField("machinePartChargeRateModifier", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartChargeRateModifier = "Capacitor";

        /// <summary>
        /// A value used to scale the charge rate multiplier
        /// with the corresponding part rating.
        /// </summary>
        [DataField("partRatingChargeRateModifier")]
        public float PartRatingChargeRateModifier = 1.5f;

        [DataField("slotId", required: true)]
        public string SlotId = string.Empty;
    }
}
