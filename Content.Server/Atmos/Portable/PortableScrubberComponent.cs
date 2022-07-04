using Content.Shared.Atmos;

namespace Content.Server.Atmos.Portable
{
    [RegisterComponent]
    public sealed class PortableScrubberComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("gasMixture")]
        public GasMixture Air { get; } = new();

        public HashSet<Gas> FilterGases = new()
        {
            Gas.CarbonDioxide,
            Gas.Plasma,
            Gas.Tritium,
            Gas.WaterVapor,
            Gas.Miasma
        };

        public float TransferRate = 1000f;
        public bool Enabled = true;
    }
}
