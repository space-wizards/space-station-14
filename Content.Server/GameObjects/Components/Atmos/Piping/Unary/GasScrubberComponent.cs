using System;
using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Unary
{
    [RegisterComponent]
    public class GasScrubberComponent : Component
    {
        public override string Name => "GasScrubber";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "pipe";

        [ViewVariables]
        public readonly HashSet<Gas> FilterGases = new()
        {
            Gas.CarbonDioxide
        };

        [ViewVariables(VVAccess.ReadWrite)]
        public ScrubberPumpDirection PumpDirection { get; set; } = ScrubberPumpDirection.Scrubbing;

        [ViewVariables(VVAccess.ReadWrite)]
        public float VolumeRate { get; set; } = 200f;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool WideNet { get; set; } = false;
    }

    public enum ScrubberPumpDirection : sbyte
    {
        Siphoning = 0,
        Scrubbing = 1,
    }
}
