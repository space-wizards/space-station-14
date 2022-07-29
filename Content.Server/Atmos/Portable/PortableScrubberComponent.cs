using Content.Shared.Atmos;

namespace Content.Server.Atmos.Portable
{
    [RegisterComponent]
    public sealed class PortableScrubberComponent : Component
    {
        /// <summary>
        /// The air inside this machine.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("gasMixture")]
        public GasMixture Air { get; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("port")]
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
            Gas.Miasma,
            Gas.NitrousOxide,
            Gas.Frezon
        };

        /// <summary>
        /// Can this scrubber hold more gas?
        /// </summary>
        public bool Full => Air.Pressure >= MaxPressure;

        /// <summary>
        /// Maximum internal pressure before it refuses to take more.
        /// </summary>
        [DataField("maxPressure")]
        public float MaxPressure = 3000f;
        [DataField("transferRate")]
        public float TransferRate = 1000f;
        public bool Enabled = true;
    }
}
