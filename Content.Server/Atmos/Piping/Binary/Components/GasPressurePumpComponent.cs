using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Binary.Components
{
    [RegisterComponent]
    public sealed partial class GasPressurePumpComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
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
        ///     Max pressure of the target gas (NOT relative to source).
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxTargetPressure")]
        public float MaxTargetPressure = Atmospherics.MaxOutputPressure;
    }
}
