using System;
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
        public VentPumpDirection? PumpDirection { get; set; }
        public VentPressureBound? PressureChecks { get; set; }
        public float? ExternalPressureBound { get; set; }
        public float? InternalPressureBound { get; set; }

        // Presets for 'dumb' air alarm modes

        public static GasVentPumpData FilterModePreset = new GasVentPumpData
        {
            Enabled = true,
            PumpDirection = VentPumpDirection.Releasing,
            PressureChecks = VentPressureBound.ExternalBound,
            ExternalPressureBound = Atmospherics.OneAtmosphere,
            InternalPressureBound = 0f
        };

        public static GasVentPumpData FillModePreset = new GasVentPumpData
        {
            Enabled = true,
            Dirty = true,
            PumpDirection = VentPumpDirection.Releasing,
            PressureChecks = VentPressureBound.ExternalBound,
            ExternalPressureBound = Atmospherics.OneAtmosphere * 50,
            InternalPressureBound = 0f
        };

        public static GasVentPumpData PanicModePreset = new GasVentPumpData
        {
            Enabled = false,
            Dirty = true,
            PumpDirection = VentPumpDirection.Releasing,
            PressureChecks = VentPressureBound.ExternalBound,
            ExternalPressureBound = Atmospherics.OneAtmosphere,
            InternalPressureBound = 0f
        };

        public static GasVentPumpData Default()
        {
            return new GasVentPumpData
            {
                Enabled = true,
                PumpDirection = VentPumpDirection.Releasing,
                PressureChecks = VentPressureBound.ExternalBound,
                ExternalPressureBound = Atmospherics.OneAtmosphere,
                InternalPressureBound = 0f
            };
        }
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
    }
}
