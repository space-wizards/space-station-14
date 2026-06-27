using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Components
{
    [Serializable, NetSerializable]
    public sealed partial class GasVentScrubberDataPayload : AtmosDeviceDataPayload
    {
        public HashSet<Gas> FilterGases { get; set; } = new(DefaultFilterGases);
        public ScrubberPumpDirection PumpDirection { get; set; } = ScrubberPumpDirection.Scrubbing;
        public float VolumeRate { get; set; } = 200f;
        public bool WideNet { get; set; } = false;
        public bool AirAlarmPanicWireCut { get; set; }

        public static HashSet<Gas> DefaultFilterGases = new()
        {
            Gas.CarbonDioxide,
            Gas.Plasma,
            Gas.Tritium,
            Gas.WaterVapor,
            Gas.Ammonia,
            Gas.NitrousOxide,
            Gas.Frezon
        };

        // Presets for 'dumb' air alarm modes

        public static GasVentScrubberDataPayload FilterModePreset = new GasVentScrubberDataPayload
        {
            Enabled = true,
            FilterGases = new(GasVentScrubberDataPayload.DefaultFilterGases),
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            WideNet = false
        };

        public static GasVentScrubberDataPayload WideFilterModePreset = new GasVentScrubberDataPayload
        {
            Enabled = true,
            FilterGases = new(GasVentScrubberDataPayload.DefaultFilterGases),
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            WideNet = true
        };

        public static GasVentScrubberDataPayload FillModePreset = new GasVentScrubberDataPayload
        {
            Enabled = false,
            Dirty = true,
            FilterGases = new(GasVentScrubberDataPayload.DefaultFilterGases),
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            WideNet = false
        };

        public static GasVentScrubberDataPayload PanicModePreset = new GasVentScrubberDataPayload
        {
            Enabled = true,
            Dirty = true,
            FilterGases = new(GasVentScrubberDataPayload.DefaultFilterGases),
            PumpDirection = ScrubberPumpDirection.Siphoning,
            VolumeRate = 200f,
            WideNet = true
        };

        public static GasVentScrubberDataPayload ReplaceModePreset = new GasVentScrubberDataPayload
        {
            Enabled = true,
            IgnoreAlarms = true,
            Dirty = true,
            FilterGases = new(GasVentScrubberDataPayload.DefaultFilterGases),
            PumpDirection = ScrubberPumpDirection.Siphoning,
            VolumeRate = 200f,
            WideNet = false
        };
    }

    [Serializable, NetSerializable]
    public sealed partial class GasVentScrubberSyncDataPayload : HandledNetworkPayload;

    [Serializable, NetSerializable]
    public sealed partial class GasVentScrubberSetDataPayload : HandledNetworkPayload
    {
        [DataField]
        public GasVentScrubberDataPayload Payload;
    }

    [Serializable, NetSerializable]
    public enum ScrubberPumpDirection : sbyte
    {
        Siphoning = 0,
        Scrubbing = 1,
    }
}
