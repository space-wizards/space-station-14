using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Trinary
{
    [RegisterComponent]
    public class GasMixerComponent : Component
    {
        public override string Name => "GasMixer";

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
        public float InletOneConcentration = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float InletTwoConcentration = 0.5f;
    }
}
