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
        public HashSet<Gas> FilterGases = new(GasVentScrubberDataPayload.DefaultFilterGases);

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

        public GasVentScrubberDataPayload ToAirAlarmData()
        {
            return new GasVentScrubberDataPayload
            {
                Enabled = Enabled,
                Dirty = IsDirty,
                FilterGases = FilterGases,
                PumpDirection = PumpDirection,
                VolumeRate = TransferRate,
                WideNet = WideNet
            };
        }

        public void FromAirAlarmData(GasVentScrubberDataPayload dataPayload)
        {
            Enabled = dataPayload.Enabled;
            IsDirty = dataPayload.Dirty;
            PumpDirection = dataPayload.PumpDirection;
            TransferRate = dataPayload.VolumeRate;
            WideNet = dataPayload.WideNet;

            if (!dataPayload.FilterGases.SequenceEqual(FilterGases))
            {
                FilterGases.Clear();
                foreach (var gas in dataPayload.FilterGases)
                    FilterGases.Add(gas);
            }
        }
    }
}
