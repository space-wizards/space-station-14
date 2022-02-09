using Content.Server.Atmos.Piping.Trinary.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Trinary.Components
{
    [RegisterComponent]
    [Friend(typeof(GasMixerSystem))]
    public sealed class GasMixerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletOne")]
        public string InletOneName = "inletOne";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletTwo")]
        public string InletTwoName = "inletTwo";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        public float TargetPressure = Atmospherics.OneAtmosphere;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletOneConcentration")]
        public float InletOneConcentration = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletTwoConcentration")]
        public float InletTwoConcentration = 0.5f;
    }
}
