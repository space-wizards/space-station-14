using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Components
{
    [Serializable, NetSerializable]
    public sealed class GasVentPumpData : IAtmosDeviceData
    {
        public bool Enabled { get; set; }
        public bool Dirty { get; set; }
        public bool IgnoreAlarms { get; set; } = false;
        public VentPumpDirection PumpDirection { get; set; } = VentPumpDirection.Releasing;
        public VentPressureBound PressureChecks { get; set; } = VentPressureBound.ExternalBound;
        public float ExternalPressureBound { get; set; } = Atmospherics.OneAtmosphere;
        public float InternalPressureBound { get; set; } = 0f;
        public bool PressureLockoutOverride { get; set; } = false;

        // Presets for 'dumb' air alarm modes

        public static GasVentPumpData FilterModePreset = new GasVentPumpData
        {
            Enabled = true,
            PumpDirection = VentPumpDirection.Releasing,
            PressureChecks = VentPressureBound.ExternalBound,
            ExternalPressureBound = Atmospherics.OneAtmosphere,
            InternalPressureBound = 0f,
            PressureLockoutOverride = false
        };

        public static GasVentPumpData FillModePreset = new GasVentPumpData
        {
            Enabled = true,
            Dirty = true,
            PumpDirection = VentPumpDirection.Releasing,
            PressureChecks = VentPressureBound.ExternalBound,
            ExternalPressureBound = Atmospherics.OneAtmosphere * 50,
            InternalPressureBound = 0f,
            PressureLockoutOverride = true
        };

        public static GasVentPumpData PanicModePreset = new GasVentPumpData
        {
            Enabled = false,
            Dirty = true,
            PumpDirection = VentPumpDirection.Releasing,
            PressureChecks = VentPressureBound.ExternalBound,
            ExternalPressureBound = Atmospherics.OneAtmosphere,
            InternalPressureBound = 0f,
            PressureLockoutOverride = false
        };

        public static GasVentPumpData ReplaceModePreset = new GasVentPumpData
        {
            Enabled = false,
            IgnoreAlarms = true,
            Dirty = true,
            PumpDirection = VentPumpDirection.Releasing,
            PressureChecks = VentPressureBound.ExternalBound,
            ExternalPressureBound = Atmospherics.OneAtmosphere,
            InternalPressureBound = 0f,
            PressureLockoutOverride = false
        };
    }

    [Serializable, NetSerializable]
    public enum VentPumpDirection : sbyte
    {
        Siphoning = 0,
        Releasing = 1,
    }

    [Flags]
    [Serializable, NetSerializable]
    public enum VentPressureBound : sbyte
    {
        NoBound       = 0,
        InternalBound = 1,
        ExternalBound = 2,
        Both = 3,
    }
}
