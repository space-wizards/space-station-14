using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Quaternary.Components
{
    [RegisterComponent]
    public sealed partial class CounterflowHeatExchangerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        public string InletName { get; set; } = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletSecondary")]
        public string InletSecondaryName { get; set; } = "inletSecondary";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outletSecondary")]
        public string OutletSecondaryName { get; set; } = "outletSecondary";

        /// <summary>
        /// Heat Transfer Coefficient (W/m²K)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("heatTransferCoefficient")]
        public float HeatTransferCoefficient { get; set; } = 50f;

        /// <summary>
        /// Heat Exchange Area (m²)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("heatExchangeArea")]

        public float HeatExchangeArea { get; set; } = 10f;
    }
}
