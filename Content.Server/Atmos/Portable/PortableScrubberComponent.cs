using Content.Shared.Atmos;

namespace Content.Server.Atmos.Portable
{
    [RegisterComponent]
    public sealed class PortableScrubberComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("gasMixture")]
        public GasMixture Air { get; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("port")]
        public string PortName { get; set; } = "port";

        public HashSet<Gas> FilterGases = new()
        {
            Gas.CarbonDioxide,
            Gas.Plasma,
            Gas.Tritium,
            Gas.WaterVapor,
            Gas.Miasma
        };

        public bool Full => Air.Pressure >= MaxPressure;

        /// <summary>
        /// Maximum internal pressure before it refuses to take more.
        /// </summary>
        public float MaxPressure = 3000f;
        public float TransferRate = 1000f;
        public bool Enabled = true;
    }
}
