using Content.Shared.Atmos;

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
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float MaxPressure = 2500;

        /// <summary>
        /// The speed at which gas is scrubbed from the environment.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float TransferRate = 800;
    }
}
