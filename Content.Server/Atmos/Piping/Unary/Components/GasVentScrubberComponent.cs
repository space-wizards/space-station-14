using System.Linq;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    [Access(typeof(GasVentScrubberSystem))]
    public sealed partial class GasVentScrubberComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = false;

        [ViewVariables]
        public bool IsDirty { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "pipe";

        [ViewVariables]
        public readonly HashSet<Gas> FilterGases = new(GasVentScrubberData.DefaultFilterGases);

        [ViewVariables(VVAccess.ReadWrite)]
        public ScrubberPumpDirection PumpDirection { get; set; } = ScrubberPumpDirection.Scrubbing;

        /// <summary>
        ///     Target volume to transfer. If <see cref="WideNet"/> is enabled, actual transfer rate will be much higher.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferRate
        {
            get => _transferRate;
            set => _transferRate = Math.Clamp(value, 0f, MaxTransferRate);
        }

        private float _transferRate = Atmospherics.MaxTransferRate;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxTransferRate")]
        public float MaxTransferRate = Atmospherics.MaxTransferRate;

        /// <summary>
        ///     As pressure difference approaches this number, the effective volume rate may be smaller than <see
        ///     cref="TransferRate"/>
        /// </summary>
        [DataField("maxPressure")]
        public float MaxPressure = Atmospherics.MaxOutputPressure;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool WideNet { get; set; } = false;

        public GasVentScrubberData ToAirAlarmData()
        {
            return new GasVentScrubberData
            {
                Enabled = Enabled,
                Dirty = IsDirty,
                FilterGases = FilterGases,
                PumpDirection = PumpDirection,
                VolumeRate = TransferRate,
                WideNet = WideNet
            };
        }

        public void FromAirAlarmData(GasVentScrubberData data)
        {
            Enabled = data.Enabled;
            IsDirty = data.Dirty;
            PumpDirection = data.PumpDirection;
            TransferRate = data.VolumeRate;
            WideNet = data.WideNet;

            if (!data.FilterGases.SequenceEqual(FilterGases))
            {
                FilterGases.Clear();
                foreach (var gas in data.FilterGases)
                    FilterGases.Add(gas);
            }
        }
    }
}
