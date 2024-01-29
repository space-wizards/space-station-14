using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Binary.Components
{
    [RegisterComponent]
    public sealed partial class GasRecyclerComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("reacting")]
        public Boolean Reacting { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        public string InletName { get; set; } = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "outlet";

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float MinTemp = 300 + Atmospherics.T0C;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float MinPressure = 30 * Atmospherics.OneAtmosphere;
    }
}
