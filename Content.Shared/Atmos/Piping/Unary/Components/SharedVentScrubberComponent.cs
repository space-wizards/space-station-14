using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Components
{
    [Serializable, NetSerializable]
    public sealed class GasVentScrubberData : IAtmosDeviceData
    {
        public bool Enabled { get; set; }
        public bool Dirty { get; set; }
        public bool IgnoreAlarms { get; set; } = false;
        public HashSet<Gas> FilterGases { get; set; } = new(DefaultFilterGases);
        public HashSet<Gas> OverflowGases { get; set; } = new(DefaultOverflowGases);
        public ScrubberPumpDirection PumpDirection { get; set; } = ScrubberPumpDirection.Scrubbing;
        public float VolumeRate { get; set; } = 200f;
        public float TargetPressure { get; set; } = Atmospherics.OneAtmosphere;
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
        public static HashSet<Gas> DefaultOverflowGases = new()
        {
            Gas.Oxygen,
            Gas.Nitrogen
        };

        // Presets for 'dumb' air alarm modes

        public static GasVentScrubberData FilterModePreset = new GasVentScrubberData
        {
            Enabled = true,
            FilterGases = new(GasVentScrubberData.DefaultFilterGases),
            OverflowGases = new(GasVentScrubberData.DefaultOverflowGases),
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            TargetPressure = Atmospherics.OneAtmosphere,
            WideNet = false
        };

        public static GasVentScrubberData WideFilterModePreset = new GasVentScrubberData
        {
            Enabled = true,
            FilterGases = new(GasVentScrubberData.DefaultFilterGases),
            OverflowGases = new(GasVentScrubberData.DefaultOverflowGases),
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            TargetPressure = Atmospherics.OneAtmosphere,
            WideNet = true
        };

        public static GasVentScrubberData FillModePreset = new GasVentScrubberData
        {
            Enabled = false,
            Dirty = true,
            FilterGases = [],
            OverflowGases = [],
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            TargetPressure = Atmospherics.OneAtmosphere,
            WideNet = false
        };

        public static GasVentScrubberData PanicModePreset = new GasVentScrubberData
        {
            Enabled = true,
            Dirty = true,
            FilterGases = new(GasVentScrubberData.DefaultFilterGases),
            OverflowGases = new(GasVentScrubberData.DefaultOverflowGases),
            PumpDirection = ScrubberPumpDirection.Siphoning,
            VolumeRate = 200f,
            TargetPressure = 0f,
            WideNet = true
        };
    }

    [Serializable, NetSerializable]
    public enum ScrubberPumpDirection : sbyte
    {
        Siphoning = 0,
        Scrubbing = 1,
    }
}
