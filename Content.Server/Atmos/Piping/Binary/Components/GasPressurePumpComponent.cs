using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Binary.Components
{
    [RegisterComponent]
    public sealed class GasPressurePumpComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        public string InletName { get; set; } = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("targetPressure")]
        public float TargetPressure { get; set; } = Atmospherics.OneAtmosphere;

        /// <summary>
        ///     If the difference between the internal and external pressure is larger than this, the device can no
        ///     longer move gas.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxTargetPressure")]
        public float MaxTargetPressure = Atmospherics.MaxOutputPressure;
    }
}
