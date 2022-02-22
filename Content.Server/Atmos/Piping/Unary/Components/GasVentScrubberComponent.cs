using System.Collections.Generic;
using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed class GasVentScrubberComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables]
        public bool IsDirty { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Welded { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "pipe";

        [ViewVariables]
        public readonly HashSet<Gas> FilterGases = GasVentScrubberData.DefaultFilterGases;

        [ViewVariables(VVAccess.ReadWrite)]
        public ScrubberPumpDirection PumpDirection { get; set; } = ScrubberPumpDirection.Scrubbing;

        [ViewVariables(VVAccess.ReadWrite)]
        public float VolumeRate { get; set; } = 200f;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool WideNet { get; set; } = false;

        public GasVentScrubberData ToAirAlarmData()
        {
            if (!IsDirty) return new GasVentScrubberData { Dirty = IsDirty };

            return new GasVentScrubberData
            {
                Enabled = Enabled,
                Dirty = IsDirty,
                FilterGases = FilterGases,
                PumpDirection = PumpDirection,
                VolumeRate = VolumeRate,
                WideNet = WideNet
            };
        }

        public void FromAirAlarmData(GasVentScrubberData data)
        {
            Enabled = data.Enabled;
            IsDirty = data.Dirty;
            PumpDirection = (ScrubberPumpDirection) data.PumpDirection!;
            VolumeRate = (float) data.VolumeRate!;
            WideNet = data.WideNet;

            if (!data.FilterGases!.SequenceEqual(FilterGases))
            {
                FilterGases.Clear();
                foreach (var gas in data.FilterGases!)
                    FilterGases.Add(gas);
            }
        }
    }
}
