using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Binary.Components
{
    [RegisterComponent]
    public sealed class GasPassiveGateComponent : Component
    {
        [DataField("enabled")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     This is the minimum difference needed to overcome the friction in the mechanism.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("frictionDifference")]
        public float FrictionPressureDifference { get; set; } = 10f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        public string InletName { get; set; } = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        public float TargetPressure { get; set; } = Atmospherics.OneAtmosphere;
    }
}
