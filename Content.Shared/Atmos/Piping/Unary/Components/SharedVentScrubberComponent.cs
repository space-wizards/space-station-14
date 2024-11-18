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
        public HashSet<Gas> PriorityGases { get; set; } = new(DefaultPriorityGases);
        public HashSet<Gas> DisabledGases { get; set; } = [];
        public ScrubberPumpDirection PumpDirection { get; set; } = ScrubberPumpDirection.Scrubbing;
        public float VolumeRate { get; set; } = 200f;
        public float TargetPressure { get; set; } = Atmospherics.OneAtmosphere;
        public bool WideNet { get; set; } = false;

        public static HashSet<Gas> DefaultPriorityGases = new()
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

        public static GasVentScrubberData FilterModePreset = new GasVentScrubberData
        {
            Enabled = true,
            PriorityGases = new(GasVentScrubberData.DefaultPriorityGases),
            DisabledGases = [],
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            TargetPressure = Atmospherics.OneAtmosphere,
            WideNet = false
        };

        public static GasVentScrubberData WideFilterModePreset = new GasVentScrubberData
        {
            Enabled = true,
            PriorityGases = new(GasVentScrubberData.DefaultPriorityGases),
            DisabledGases = [],
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            TargetPressure = Atmospherics.OneAtmosphere,
            WideNet = true
        };

        public static GasVentScrubberData FillModePreset = new GasVentScrubberData
        {
            Enabled = false,
            Dirty = true,
            PriorityGases = [],
            DisabledGases = new(Enum.GetValues<Gas>()),
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            TargetPressure = Atmospherics.OneAtmosphere,
            WideNet = false
        };

        public static GasVentScrubberData PanicModePreset = new GasVentScrubberData
        {
            Enabled = true,
            Dirty = true,
            PriorityGases = new(GasVentScrubberData.DefaultPriorityGases),
            DisabledGases = [],
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
