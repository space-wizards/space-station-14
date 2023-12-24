using Content.Shared.Atmos;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Atmos.Portable
{
    [RegisterComponent]
    public sealed partial class PortableScrubberComponent : Component
    {
        /// <summary>
        /// The air inside this machine.
        /// </summary>
        [DataField("gasMixture"), ViewVariables(VVAccess.ReadWrite)]
        public GasMixture Air { get; private set; } = new();

        [DataField("port"), ViewVariables(VVAccess.ReadWrite)]
        public string PortName { get; set; } = "port";

        /// <summary>
        /// Which gases this machine will scrub out.
        /// Unlike fixed scrubbers controlled by an air alarm,
        /// this can't be changed in game.
        /// </summary>
        [DataField("filterGases")]
        public HashSet<Gas> FilterGases = new()
        {
            Gas.CarbonDioxide,
            Gas.Plasma,
            Gas.Tritium,
            Gas.WaterVapor,
            Gas.Ammonia,
            Gas.NitrousOxide,
            Gas.Frezon
        };

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled = true;

        /// <summary>
        /// Maximum internal pressure before it refuses to take more.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxPressure = 2500;

        /// <summary>
        /// The base amount of maximum internal pressure
        /// </summary>
        [DataField("baseMaxPressure")]
        public float BaseMaxPressure = 2500;

        /// <summary>
        /// The machine part that modifies the maximum internal pressure
        /// </summary>
        [DataField("machinePartMaxPressure", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartMaxPressure = "MatterBin";

        /// <summary>
        /// How much the <see cref="MachinePartMaxPressure"/> will affect the pressure.
        /// The value will be multiplied by this amount for each increasing part tier.
        /// </summary>
        [DataField("partRatingMaxPressureModifier")]
        public float PartRatingMaxPressureModifier = 1.5f;

        /// <summary>
        /// The speed at which gas is scrubbed from the environment.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferRate = 800;

        /// <summary>
        /// The base speed at which gas is scrubbed from the environment.
        /// </summary>
        [DataField("baseTransferRate")]
        public float BaseTransferRate = 800;

        /// <summary>
        /// The machine part which modifies the speed of <see cref="TransferRate"/>
        /// </summary>
        [DataField("machinePartTransferRate", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartTransferRate = "Manipulator";

        /// <summary>
        /// How much the <see cref="MachinePartTransferRate"/> will modify the rate.
        /// The value will be multiplied by this amount for each increasing part tier.
        /// </summary>
        [DataField("partRatingTransferRateModifier")]
        public float PartRatingTransferRateModifier = 1.4f;
    }
}
