using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Binary.Components
{
    [RegisterComponent]
    public class GasVolumePumpComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Overclocked { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        public string InletName { get; set; } = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferRate { get; set; } = Atmospherics.MaxTransferRate;

        [DataField("leakRatio")]
        public float LeakRatio { get; set; } = 0.1f;

        [DataField("lowerThreshold")]
        public float LowerThreshold { get; set; } = 0.01f;

        [DataField("higherThreshold")]
        public float HigherThreshold { get; set; } = 9000f;

        [DataField("overclockThreshold")]
        public float OverclockThreshold { get; set; } = 1000f;
    }
}
