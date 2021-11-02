using System.Collections.Generic;
using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor.Components;
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
        public readonly HashSet<Gas> FilterGases = DefaultFilterGases;

        public static HashSet<Gas> DefaultFilterGases = new()
        {
            Gas.CarbonDioxide,
            Gas.Plasma,
            Gas.Tritium,
            Gas.WaterVapor,
        };

        [ViewVariables(VVAccess.ReadWrite)]
        public ScrubberPumpDirection PumpDirection { get; set; } = ScrubberPumpDirection.Scrubbing;

        [ViewVariables(VVAccess.ReadWrite)]
        public float VolumeRate { get; set; } = 200f;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool WideNet { get; set; } = false;

        public GasVentScrubberData ToAirAlarmData() => new GasVentScrubberData
        {
            Enabled = Enabled,
            FilterGases = FilterGases,
            PumpDirection = PumpDirection,
            VolumeRate = VolumeRate,
            WideNet = WideNet
        };

        public void FromAirAlarmData(GasVentScrubberData data)
        {
            Enabled = data.Enabled;
            PumpDirection = data.PumpDirection;
            VolumeRate = data.VolumeRate;
            WideNet = data.WideNet;

            if (!data.FilterGases.SequenceEqual(FilterGases))
            {
                FilterGases.Clear();
                foreach (var gas in data.FilterGases)
                    FilterGases.Add(gas);
            }
        }

        // Presets for 'dumb' air alarm modes

        public static GasVentScrubberData FilterModePreset = new GasVentScrubberData
        {
            Enabled = true,
            FilterGases = DefaultFilterGases,
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            WideNet = false
        };

        public static GasVentScrubberData FillModePreset = new GasVentScrubberData
        {
            Enabled = false,
            FilterGases = DefaultFilterGases,
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            WideNet = false
        };

        public static GasVentScrubberData PanicModePreset = new GasVentScrubberData
        {
            Enabled = true,
            FilterGases = DefaultFilterGases,
            PumpDirection = ScrubberPumpDirection.Siphoning,
            VolumeRate = 200f,
            WideNet = false
        };
    }
}
