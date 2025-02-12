using Content.Shared.Atmos;
using Content.Shared.Guidebook;

namespace Content.Server.Atmos.Piping.Binary.Components
{
    [RegisterComponent]
    public sealed partial class GasVolumePumpComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool Enabled { get; set; } = true;

        [DataField]
        public bool Blocked { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Overclocked { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        public string InletName { get; set; } = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float TransferRate { get; set; } = Atmospherics.MaxTransferRate;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float MaxTransferRate { get; set; } = Atmospherics.MaxTransferRate;

        [DataField]
        public float LeakRatio { get; set; } = 0.1f;

        [DataField]
        public float LowerThreshold { get; set; } = 0.01f;

        [DataField]
        [GuidebookData]
        public float HigherThreshold { get; set; } = DefaultHigherThreshold;
        public static readonly float DefaultHigherThreshold = 2 * Atmospherics.MaxOutputPressure;

        [DataField]
        public float OverclockThreshold { get; set; } = 1000;

        [DataField]
        public float LastMolesTransferred;
    }
}
