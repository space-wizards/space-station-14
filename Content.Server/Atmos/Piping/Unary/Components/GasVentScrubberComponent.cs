using System.Collections.Generic;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public class GasVentScrubberComponent : Component
    {
        public override string Name => "GasVentScrubber";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Welded { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "pipe";

        [ViewVariables]
        public readonly HashSet<Gas> FilterGases = new()
        {
            Gas.CarbonDioxide,
            Gas.Plasma,
            Gas.Tritium,
            Gas.WaterVapor
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
