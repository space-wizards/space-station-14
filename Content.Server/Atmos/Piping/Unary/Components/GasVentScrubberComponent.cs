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
        /// <summary>
        /// Identifies if the device is enabled by an air alarm. Does not indicate if the device is powered.
        /// By default, all air scrubbers start enabled, whether linked to an alarm or not.
        /// </summary>
        [DataField]
        public bool Enabled { get; set; } = true;

        [DataField]
        public bool IsDirty { get; set; } = false;

        [DataField("outlet")]
        public string OutletName { get; set; } = "pipe";

        [DataField]
        public HashSet<Gas> FilterGases = new(GasVentScrubberData.DefaultFilterGases);

        [DataField]
        public HashSet<Gas> OverflowGases = new(GasVentScrubberData.DefaultOverflowGases);

        [DataField]
        public ScrubberPumpDirection PumpDirection { get; set; } = ScrubberPumpDirection.Scrubbing;

        /// <summary>
        ///     Target volume to transfer. If <see cref="WideNet"/> is enabled, actual transfer rate will be much higher.
        /// </summary>
        [DataField]
        public float TransferRate
        {
            get => _transferRate;
            set => _transferRate = Math.Clamp(value, 0f, MaxTransferRate);
        }

        private float _transferRate = Atmospherics.MaxTransferRate;

        /// <summary>
        ///     Target pressure. If the pressure is below this value only priority gases are getting scrubbed.
        /// </summary>
        [DataField]
        public float TargetPressure
        {
            get => _targetPressure;
            set => _targetPressure = Math.Max(value, 0f);
        }
        private float _targetPressure = Atmospherics.OneAtmosphere;

        [DataField]
        public float MaxTransferRate = Atmospherics.MaxTransferRate;

        /// <summary>
        ///     As pressure difference approaches this number, the effective volume rate may be smaller than <see
        ///     cref="TransferRate"/>
        /// </summary>
        [DataField]
        public float MaxPressure = Atmospherics.MaxOutputPressure;

        [DataField]
        public bool WideNet { get; set; } = false;

        public GasVentScrubberData ToAirAlarmData()
        {
            return new GasVentScrubberData
            {
                Enabled = Enabled,
                Dirty = IsDirty,
                FilterGases = FilterGases,
                OverflowGases = OverflowGases,
                PumpDirection = PumpDirection,
                VolumeRate = TransferRate,
                TargetPressure = TargetPressure,
                WideNet = WideNet
            };
        }

        public void FromAirAlarmData(GasVentScrubberData data)
        {
            Enabled = data.Enabled;
            IsDirty = data.Dirty;
            PumpDirection = data.PumpDirection;
            TransferRate = data.VolumeRate;
            TargetPressure = data.TargetPressure;
            WideNet = data.WideNet;

            if (!data.FilterGases.SequenceEqual(FilterGases))
            {
                FilterGases.Clear();
                foreach (var gas in data.FilterGases)
                    FilterGases.Add(gas);
            }
            if (!data.OverflowGases.SequenceEqual(OverflowGases))
            {
                OverflowGases.Clear();
                foreach (var gas in data.OverflowGases)
                    OverflowGases.Add(gas);
            }
        }
    }
}
